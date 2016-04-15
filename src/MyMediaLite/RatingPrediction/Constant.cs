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
using System.Globalization;
using System.IO;
using MyMediaLite.IO;

namespace MyMediaLite.RatingPrediction
{
	/// <summary>Uses a constant rating value for prediction</summary>
	/// <remarks>
	/// This recommender supports incremental updates.
	/// Updates are just ignored, because the prediction is always the same.
	/// </remarks>
	public class Constant : IncrementalRatingPredictor
	{
		/// <summary>the constant rating</summary>
		public float ConstantRating { get; set; }

		/// <summary>Default constructor</summary>
		public Constant()
		{
			ConstantRating = 1.0f;
		}

		///
		public override void Train() { }

		///
		public override bool CanPredict(int user_id, int item_id)
		{
			return true;
		}

		///
		public override float Predict(int user_id, int item_id)
		{
			return ConstantRating;
		}

		///
		public override void SaveModel(string filename) { /* do nothing */ }

		///
		public override void LoadModel(string filename) { /* do nothing */ }

		///
		public override string ToString()
		{
			return string.Format(
				"{0} constant_rating={1}",
				this.GetType().Name, ConstantRating);
		}
	}
}