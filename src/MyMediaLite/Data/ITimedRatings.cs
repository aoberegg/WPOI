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
using System.Collections.Generic;

namespace MyMediaLite.Data
{
	/// <summary>Interface for rating datasets with time information</summary>
	public interface ITimedRatings : IRatings, ITimedDataSet
	{
		/// <summary>add a rating event including time information</summary>
		/// <remarks>
		/// It is up to the user of a class implementing this interface to decide whether the DateTime
		/// object represent local time, UTC, or any other time.
		/// </remarks>
		/// <param name="user_id">the user ID</param>
		/// <param name="item_id">the item ID</param>
		/// <param name="rating">the rating value</param>
		/// <param name="time">A <see cref="DateTime"/> specifying the time of the rating event</param>
		void Add(int user_id, int item_id, float rating, DateTime time);

		/// <summary>
		/// Gets the times.
		/// </summary>
		/// <returns>The times.</returns>
		/// <param name="user_id">User identifier.</param>
		/// <param name="item_id">Item identifier.</param>
		IList<DateTime> getTimes (int user_id, int item_id);


		/// <summary>all times in the dataset</summary>
		IList<DateTime> AllTimes { get; }

		/// <summary>
		/// Gets the times of a user.
		/// </summary>
		/// <returns>The times.</returns>
		/// <param name="user_id">User identifier.</param>
		IList<DateTime> getTimesOfUser (int user_id);

		/// <summary>
		/// Gets the checkin count.
		/// </summary>
		/// <returns>The checkin count.</returns>
		/// <param name="user_id">User identifier.</param>
		/// <param name="item_id">Item identifier.</param>
		int getCheckinCount (int user_id, int item_id);
			
		/// <summary>
		/// Gets the items of user.
		/// </summary>
		/// <returns>The items of user.</returns>
		/// <param name="user_id">User identifier.</param>
		IList<int> getItemsOfUser (int user_id);

		/// <summary>
		/// Removes the users.
		/// </summary>
		/// <param name="users">Users.</param>
		void RemoveUsers(IList<int> users);

		/// <summary>
		/// Gets the items of all users with Time.
		/// </summary>
		/// <returns>The items user dict.</returns>
		Dictionary<int,IList<Tuple<int,DateTime>>> getItemsUserDictWithTime();

		/// <summary>
		/// Gets the items of all users.
		/// </summary>
		/// <returns>The items user dict.</returns>
		Dictionary<int,IList<int>> getItemsUserDict ();

		/// <summary>
		/// Gets the times of all users.
		/// </summary>
		/// <returns>The times user dict.</returns>
		Dictionary<int,IList<DateTime>> getTimesUserDict ();

		/// <summary>
		/// Gets the times of all users.
		/// </summary>
		/// <returns>The times user dict.</returns>
		Dictionary<int,IList<DateTime>> getTimesItemDict ();

	}
}