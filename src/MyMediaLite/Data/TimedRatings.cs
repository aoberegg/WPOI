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
// You should have received a copy of the GNU General Public License
// along with MyMediaLite.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MyMediaLite.Data
{
	/// <summary>Data structure for storing ratings with time information</summary>
	/// <remarks>
	/// <para>This data structure supports incremental updates.</para>
	/// <para>
	/// Loading the Netflix Prize data set (100,000,000 ratings) into this data structure requires about 3.2 GB of memory.
	/// </para>
	/// </remarks>
	[Serializable()]
	public class TimedRatings : Ratings, ITimedRatings
	{

		public virtual Dictionary<int,IList<DateTime>> getTimesItemDict(){
			Dictionary<int, IList<DateTime>> times = new Dictionary<int, IList<DateTime>> ();

			for (int index = 0; index < Values.Count; index++) {
				if (!times.ContainsKey (Items[index]))
					times.Add (Items[index], new List<DateTime> ());
				times[Items[index]].Add (Times [index]);
			}
			return times;
		}

		/// <summary>
		/// Gets the times of a user.
		/// </summary>
		/// <returns>The times.</returns>
		/// <param name="user_id">User identifier.</param>
		public virtual IList<DateTime> getTimesOfUser(int user_id){
			IList<DateTime> dateTimes = new List<DateTime> ();
			for (int index = 0; index < Values.Count; index++){
				if (Users[index] == user_id)
					dateTimes.Add(Times[index]);
			}
			if (dateTimes.Count == 0)
				throw new KeyNotFoundException(string.Format("no rating for user {0}found.", user_id));
			return dateTimes;
		}

		/// <summary>
		/// Gets the times of a user and item.
		/// </summary>
		/// <returns>The times.</returns>
		/// <param name="user_id">User identifier.</param>
		/// <param name="item_id">Item identifier.</param>
		public virtual IList<DateTime> getTimes(int user_id, int item_id)
		{
			IList<DateTime> dateTimes = new List<DateTime> ();
			for (int index = 0; index < Values.Count; index++){
				if (Users[index] == user_id && Items[index] == item_id)
						dateTimes.Add(Times[index]);
			}
			if (dateTimes.Count == 0)
				throw new KeyNotFoundException(string.Format("no rating for user {0} and item {1} found.", user_id, item_id));
			return dateTimes;
		}

		/// <summary>
		/// Gets the checkin count.
		/// </summary>
		/// <returns>The checkin count.</returns>
		/// <param name="user_id">User identifier.</param>
		/// <param name="item_id">Item identifier.</param>
		public virtual int getCheckinCount(int user_id, int item_id){
			int count = 0;
			for (int index = 0; index < Values.Count; index++){
				if (Users [index] == user_id && Items [index] == item_id)
					count += 1;
			}
			return count;
		}
			
		/// <summary>
		/// Gets the items of user.
		/// </summary>
		/// <returns>The items of user.</returns>
		/// <param name="user_id">User identifier.</param>
		public virtual IList<int> getItemsOfUser (int user_id){
			IList<int> items = new List<int> ();
			for (int index = 0; index < Values.Count; index++) {
				foreach (int item_id in AllItems) {
					if (Users [index] == user_id && Items [index] == item_id) {
						items.Add (item_id);
						continue;
					}
				}
			}
			return items;

		}

		public virtual Dictionary<int,IList<Tuple<int,DateTime>>> getItemsUserDictWithTime(){
			Dictionary<int,IList<Tuple<int,DateTime>>> items = new Dictionary<int, IList<Tuple<int,DateTime>>> ();
			for (int index = 0; index < Values.Count; index++) {
				//						if (Users [index] == user_id && Items [index] == item_id) {
				if (!items.ContainsKey (Users[index]))
					items.Add (Users[index], new List<Tuple<int,DateTime>> ());
				items[Users[index]].Add (Tuple.Create(Items [index], Times[index]));
				//						}
			}
			//				}
			//			}
			return items;
		}

		/// <summary>
		/// Gets the items of all users.
		/// </summary>
		/// <returns>The items user dict.</returns>
		public virtual Dictionary<int,IList<int>> getItemsUserDict (){
			Dictionary<int, IList<int>> items = new Dictionary<int, IList<int>> ();

//			foreach(int user_id in AllUsers){
//				foreach (int item_id in AllItems) {
			for (int index = 0; index < Values.Count; index++) {
//						if (Users [index] == user_id && Items [index] == item_id) {
				if (!items.ContainsKey (Users[index]))
					items.Add (Users[index], new List<int> ());
				items[Users[index]].Add (Items [index]);
							continue;
//						}
			}
//				}
//			}
			return items;
		}

		/// <summary>
		/// Gets the times of all users.
		/// </summary>
		/// <returns>The times user dict.</returns>
		public virtual Dictionary<int,IList<DateTime>> getTimesUserDict (){
			Dictionary<int, IList<DateTime>> times = new Dictionary<int, IList<DateTime>> ();

//			foreach(int user_id in AllUsers){
//				foreach (DateTime time_id in AllTimes) {
			for (int index = 0; index < Values.Count; index++) {
//						if (Users [index] == user_id && Times [index] == time_id) {
					if (!times.ContainsKey (Users[index]))
						times.Add (Users[index], new List<DateTime> ());
					times[Users[index]].Add (Times [index]);
					continue;
//						}
			}
//				}
//			}
			return times;
		}


		///
		public IList<DateTime> Times { get; protected set; }

		///
		public DateTime EarliestTime { get; protected set; }

		///
		public DateTime LatestTime { get; protected set; }

		/// <summary>Default constructor</summary>
		public TimedRatings() : base()
		{
			Times = new List<DateTime>();
			EarliestTime = DateTime.MaxValue;
			LatestTime = DateTime.MinValue;
		}

		///
		public TimedRatings(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			Times = (List<DateTime>) info.GetValue("Times", typeof(List<DateTime>));

			EarliestTime = Times.Min();
			LatestTime   = Times.Max();
		}

		///
		public override void Add(int user_id, int item_id, float rating)
		{
			throw new NotSupportedException();
		}

		///
		public virtual void Add(int user_id, int item_id, float rating, DateTime time)
		{
			Users.Add(user_id);
			Items.Add(item_id);
			Values.Add(rating);
			Times.Add(time);

			int pos = Users.Count - 1;

			if (user_id > MaxUserID)
				MaxUserID = user_id;
			if (item_id > MaxItemID)
				MaxItemID = item_id;

			// TODO speed up time stuff
			if (time < EarliestTime)
				EarliestTime = time;
			if (time > LatestTime)
				LatestTime = time;

			// update index data structures if necessary
			if (by_user != null)
			{
				for (int u = by_user.Count; u <= user_id; u++)
					by_user.Add(new List<int>());
				by_user[user_id].Add(pos);
			}
			if (by_item != null)
			{
				for (int i = by_item.Count; i <= item_id; i++)
					by_item.Add(new List<int>());
				by_item[item_id].Add(pos);
			}
		}

		///
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Times", this.Times);
		}

		///
		public override void RemoveUser(int user_id)
		{
			var items_to_update = new HashSet<int>();

			for (int index = 0; index < Count; index++)
				if (Users[index] == user_id)
				{
					items_to_update.Add(Items[index]);

					Users.RemoveAt(index);
					Items.RemoveAt(index);
					Values.RemoveAt(index);
					Times.RemoveAt (index);

					index--; // avoid missing an entry
				}

			UpdateCountsAndIndices(new HashSet<int>() { user_id }, items_to_update);

			if (MaxUserID == user_id)
				MaxUserID--;
		}

		///
		public void RemoveUsers(IList<int> users)
		{
			var items_to_update = new HashSet<int>();
			Dictionary<int ,HashSet<int>> user_items_to_update = new Dictionary<int,HashSet<int>>();
			int i = 0;
			Console.WriteLine (Count);
			for (int index = 0; index < Count; index++) {
				if(i %1000 == 0)
					Console.WriteLine (i);
				i++;
				if (users.Contains (Users [index])) {
					if (!user_items_to_update.ContainsKey (Users [index]))
						user_items_to_update.Add (Users [index], new HashSet<int> ());
					if (!user_items_to_update [Users [index]].Contains (Items [index]))
						user_items_to_update [Users [index]].Add (Items [index]);

					Users.RemoveAt (index);
					Items.RemoveAt (index);
					Values.RemoveAt (index);
					Times.RemoveAt (index);

					index--; // avoid missing an entry
				}
			}
			foreach (int user_id in users)
				UpdateCountsAndIndices(new HashSet<int>() { user_id }, user_items_to_update[user_id]);
			MaxUserID = Users.Max();
		}

		/// <summary>all item IDs in the dataset</summary>
		public virtual IList<DateTime> AllTimes { 
			get {
				var result_set = new HashSet<DateTime>();
				for (int index = 0; index < Times.Count; index++)
					result_set.Add(Times[index]);
				return result_set.ToArray();
			}
		}

	}
}