// Copyright (C) 2011, 2012 Zeno Gantner
//
// This file is part of MyMediaLite.
//
// MyMediaLite is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MyMediaLite is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with MyMediaLite.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using MyMediaLite.Data;
using System.Linq;
using C5;
using System.Collections.Generic;
using MyMediaLite.DataType;

namespace MyMediaLite.RatingPrediction
{
	/// <summary>Abstract class for time-aware rating predictors</summary>
	/// <exception cref='ArgumentException'>
	/// Is thrown when an argument passed to a method is invalid.
	/// </exception>
	public abstract class TimeAwareRatingPredictor : RatingPredictor, ITimeAwareRatingPredictor
	{
		public string connection_string;

		/// <summary>the rating data, including time information</summary>
		public virtual ITimedRatings TimedRatings
		{
			get { return timed_ratings; }
			set {
				timed_ratings = value;
				Ratings = value;
			}
		}
		/// <summary>rating data, including time information</summary>
		protected ITimedRatings timed_ratings;

		///
		public override IRatings Ratings
		{
			get { return ratings; }
			set {
				if (!(value is ITimedRatings))
					throw new ArgumentException("Ratings must be of type ITimedRatings.");

				base.Ratings = value;
				timed_ratings = (ITimedRatings) value;
			}
		}
			

		///
		public abstract float Predict(int user_id, int item_id, DateTime time);

		///
		public virtual System.Collections.Generic.IList<Tuple<int, float>> RecommendTime(
			int user_id, DateTime time ,int n = -1,
			System.Collections.Generic.ICollection<int> ignore_items = null,
			System.Collections.Generic.ICollection<int> candidate_items = null)
		{
			if (candidate_items == null)
				candidate_items = Enumerable.Range(0, MaxItemID - 1).ToList();
			if (ignore_items == null)
				ignore_items = new int[0];

			System.Collections.Generic.IList<Tuple<int, float>> ordered_items;

			if (n == -1)
			{
				var scored_items = new List<Tuple<int, float>>();
				foreach (int item_id in candidate_items)
					if (!ignore_items.Contains(item_id))
					{
						float score = Predict(user_id, item_id,time);
						if (score > float.MinValue)
							scored_items.Add(Tuple.Create(item_id, score));
					}
				ordered_items = scored_items.OrderByDescending(x => x.Item2).ToArray();
			}
			else
			{
				var comparer = new DelegateComparer<Tuple<int, float>>( (a, b) => a.Item2.CompareTo(b.Item2) );
				var heap = new IntervalHeap<Tuple<int, float>>(n, comparer);
				float min_relevant_score = float.MinValue;

				foreach (int item_id in candidate_items)
					if (!ignore_items.Contains(item_id))
					{
						float score = Predict(user_id, item_id, time);
						if (score > min_relevant_score)
						{
							heap.Add(Tuple.Create(item_id, score));
							if (heap.Count > n)
							{
								heap.DeleteMin();
								min_relevant_score = heap.FindMin().Item2;
							}
						}
					}

				ordered_items = new Tuple<int, float>[heap.Count];
				for (int i = 0; i < ordered_items.Count; i++)
					ordered_items[i] = heap.DeleteMax();
			}

			return ordered_items;
		}
	}
}

