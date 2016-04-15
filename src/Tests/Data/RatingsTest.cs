// Copyright (C) 2010 Christina Lichtenthäler
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

using System;
using System.Collections.Generic;
using System.Linq;
using MyMediaLite.Data;
using NUnit.Framework;

namespace Tests.Data
{
	[TestFixture()]
	public class RatingsTest
	{
		[Test()] public void TestMinRatingMaxRating()
		{
			var ratings = new Ratings();
			ratings.Add(1, 4, 0.3f);
			ratings.Add(1, 8, 0.2f);
			ratings.Add(2, 4, 0.2f);
			ratings.Add(2, 2, 0.6f);
			ratings.Add(2, 5, 0.4f);
			ratings.Add(3, 7, 0.2f);
			ratings.Add(6, 3, 0.3f);

			Assert.AreEqual(0.6f, ratings.Scale.Max);
			Assert.AreEqual(0.2f, ratings.Scale.Min);
		}

		[Test()] public void TestMaxUserIDMaxItemID()
		{
			var ratings = new Ratings();
			ratings.Add(1, 4, 0.3f);
			ratings.Add(1, 8, 0.2f);
			ratings.Add(2, 4, 0.2f);
			ratings.Add(2, 2, 0.6f);
			ratings.Add(2, 5, 0.4f);
			ratings.Add(3, 7, 0.2f);
			ratings.Add(6, 3, 0.3f);

			Assert.AreEqual(6, ratings.MaxUserID);
			Assert.AreEqual(8, ratings.MaxItemID);
		}

		[Test()] public void TestAdd()
		{
			var ratings = new Ratings();
			ratings.Add(1, 4, 0.3f);
			ratings.Add(1, 8, 0.2f);
			ratings.Add(2, 4, 0.2f);
			ratings.Add(2, 2, 0.6f);
			ratings.Add(2, 5, 0.4f);
			ratings.Add(3, 7, 0.2f);
			ratings.Add(6, 3, 0.3f);

			Assert.AreEqual(0.4f, ratings[2, 5]);
			Assert.AreEqual(0.3f, ratings[1, 4]);
			Assert.AreEqual(0.3f, ratings[6, 3]);
			Assert.AreEqual(7, ratings.Count);
		}

		[Test()] public void TestAddAndRemove()
		{
			var ratings = new Ratings();
			ratings.Add(1, 4, 0.3f);
			ratings.Add(1, 8, 0.2f);
			ratings.Add(2, 4, 0.2f);
			ratings.Add(2, 2, 0.6f);
			ratings.Add(2, 5, 0.4f);
			ratings.Add(3, 7, 0.2f);
			ratings.Add(6, 3, 0.3f);

			Assert.AreEqual(0.4f, ratings[2, 5]);
			Assert.AreEqual(0.3f, ratings[1, 4]);
			Assert.AreEqual(0.3f, ratings[6, 3]);
			Assert.AreEqual(7, ratings.Count);

			ratings.RemoveAt(ratings.GetIndex(1, 4));
			Assert.AreEqual(6, ratings.Count);
			ratings.RemoveAt(ratings.GetIndex(1, 8));
			Assert.AreEqual(5, ratings.Count);
			ratings.RemoveAt(ratings.GetIndex(2, 5));
			Assert.AreEqual(4, ratings.Count);
			ratings.RemoveAt(ratings.GetIndex(2, 2));
			Assert.AreEqual(3, ratings.Count);
			ratings.RemoveAt(ratings.GetIndex(2, 4));
			Assert.AreEqual(2, ratings.Count);
			ratings.RemoveAt(ratings.GetIndex(3, 7));
			Assert.AreEqual(1, ratings.Count);
			ratings.RemoveAt(ratings.GetIndex(6, 3));
			Assert.AreEqual(0, ratings.Count);
		}

		[Test()] public void TestRemoveAt()
		{
			var ratings = new Ratings();
			ratings.Add(1, 4, 0.3f);
			ratings.Add(1, 8, 0.2f);
			ratings.Add(2, 4, 0.2f);
			ratings.Add(2, 2, 0.6f);
			ratings.Add(2, 5, 0.4f);
			ratings.Add(3, 7, 0.2f);
			ratings.Add(3, 3, 0.3f);
			ratings.Add(6, 3, 0.3f);

			Assert.AreEqual(8, ratings.Count);
			Assert.AreEqual(0.4f, ratings[2, 5]);
			Assert.AreEqual(3, ratings.ByUser[2].Count);
			Assert.AreEqual(1, ratings.ByUser[6].Count);
			Assert.AreEqual(2, ratings.ByItem[3].Count);
			Assert.AreEqual(1, ratings.ByItem[5].Count);
			Assert.AreEqual(3, ratings.CountByUser[2]);
			Assert.AreEqual(1, ratings.CountByUser[6]);
			Assert.AreEqual(2, ratings.CountByItem[3]);
			Assert.AreEqual(1, ratings.CountByItem[5]);
			Assert.AreEqual(8, ratings.RandomIndex.Count);

			ratings.RemoveAt(ratings.GetIndex(2, 5));
			Assert.AreEqual(7, ratings.Count);
			Assert.AreEqual(2, ratings.ByUser[2].Count);
			Assert.AreEqual(0, ratings.ByItem[5].Count);
			Assert.AreEqual(2, ratings.CountByUser[2]);
			Assert.AreEqual(0, ratings.CountByItem[5]);
			Assert.AreEqual(7, ratings.RandomIndex.Count);

			ratings.RemoveAt(ratings.GetIndex(6, 3));
			Assert.AreEqual(6, ratings.Count);
			Assert.AreEqual(0, ratings.ByUser[6].Count);
			Assert.AreEqual(1, ratings.ByItem[3].Count);
			Assert.AreEqual(0, ratings.CountByUser[6]);
			Assert.AreEqual(1, ratings.CountByItem[3]);
			Assert.AreEqual(6, ratings.RandomIndex.Count);

			float r;
			Assert.IsFalse(ratings.TryGet(2, 5, out r));
		}

		[Test()] public void TestRemoveUser()
		{
			var ratings = new Ratings();
			ratings.Add(1, 4, 0.3f);
			ratings.Add(1, 8, 0.2f);
			ratings.Add(2, 4, 0.2f);
			ratings.Add(2, 2, 0.6f);
			ratings.Add(2, 5, 0.4f);
			ratings.Add(3, 7, 0.2f);
			ratings.Add(3, 3, 0.3f);

			Assert.AreEqual(7, ratings.Count);
			Assert.AreEqual(0.4f, ratings[2, 5]);
			Assert.AreEqual(1, ratings.ByItem[2].Count);
			Assert.AreEqual(2, ratings.ByItem[4].Count);
			Assert.AreEqual(1, ratings.ByItem[5].Count);
			Assert.AreEqual(1, ratings.CountByItem[2]);
			Assert.AreEqual(2, ratings.CountByItem[4]);
			Assert.AreEqual(1, ratings.CountByItem[5]);

			ratings.RemoveUser(2);
			Assert.AreEqual(4, ratings.Count);
			Assert.AreEqual(0, ratings.ByItem[2].Count);
			Assert.AreEqual(1, ratings.ByItem[4].Count);
			Assert.AreEqual(0, ratings.ByItem[5].Count);
			Assert.AreEqual(0, ratings.CountByItem[2]);
			Assert.AreEqual(1, ratings.CountByItem[4]);
			Assert.AreEqual(0, ratings.CountByItem[5]);

			float rating;
			Assert.IsFalse(ratings.TryGet(2, 5, out rating));
			Assert.IsFalse(ratings.TryGet(2, 4, out rating));
			Assert.IsFalse(ratings.TryGet(2, 2, out rating));
		}

		[Test()] public void TestRemoveItem()
		{
			var ratings = new Ratings();
			ratings.Add(1, 4, 0.3f);
			ratings.Add(1, 8, 0.2f);
			ratings.Add(2, 4, 0.2f);
			ratings.Add(2, 2, 0.6f);
			ratings.Add(2, 5, 0.4f);
			ratings.Add(3, 4, 0.2f);
			ratings.Add(3, 3, 0.3f);

			Assert.AreEqual(7, ratings.Count);
			Assert.AreEqual(0.2f, ratings[2, 4]);
			Assert.AreEqual(2, ratings.ByUser[1].Count);
			Assert.AreEqual(3, ratings.ByUser[2].Count);
			Assert.AreEqual(2, ratings.ByUser[3].Count);
			Assert.AreEqual(2, ratings.CountByUser[1]);
			Assert.AreEqual(3, ratings.CountByUser[2]);
			Assert.AreEqual(2, ratings.CountByUser[3]);

			ratings.RemoveItem(4);
			Assert.AreEqual(4, ratings.Count);
			Assert.AreEqual(1, ratings.ByUser[1].Count);
			Assert.AreEqual(2, ratings.ByUser[2].Count);
			Assert.AreEqual(1, ratings.ByUser[3].Count);
			Assert.AreEqual(1, ratings.CountByUser[1]);
			Assert.AreEqual(2, ratings.CountByUser[2]);
			Assert.AreEqual(1, ratings.CountByUser[3]);

			float r;
			Assert.IsFalse(ratings.TryGet(1, 4, out r));
			Assert.IsFalse(ratings.TryGet(2, 4, out r));
			Assert.IsFalse(ratings.TryGet(3, 4, out r));
		}

		[Test()] public void TestIndex()
		{
			var ratings = new Ratings();
			ratings.Add(1, 4, 0.3f);
			ratings.Add(1, 8, 0.2f);
			ratings.Add(2, 4, 0.2f);
			ratings.Add(2, 2, 0.6f);
			ratings.Add(2, 5, 0.4f);
			ratings.Add(3, 4, 0.2f);
			ratings.Add(3, 3, 0.3f);
			ratings.Add(6, 3, 0.3f);

			// test index[,]
			Assert.AreEqual(0.3f, ratings[1, 4]);
			Assert.AreEqual(0.2f, ratings[1, 8]);
			Assert.AreEqual(0.6f, ratings[2, 2]);
		}

		[Test()] public void TestByUserByItem()
		{
			var ratings = new Ratings();
			ratings.Add(1, 4, 0.3f);
			ratings.Add(1, 8, 0.2f);
			ratings.Add(2, 4, 0.2f);
			ratings.Add(2, 2, 0.6f);
			ratings.Add(2, 5, 0.4f);
			ratings.Add(3, 7, 0.2f);
			ratings.Add(6, 3, 0.3f);

			Assert.IsTrue(new HashSet<int>( new int[] { 0, 1 } ).SetEquals(ratings.ByUser[1]));
			Assert.IsTrue(new HashSet<int>( new int[] { 0, 2 } ).SetEquals(ratings.ByItem[4]));
		}

		[Test()] public void TestCountByUserCountByItem()
		{
			var ratings = new Ratings();
			ratings.Add(1, 4, 0.3f);
			ratings.Add(1, 8, 0.2f);
			ratings.Add(2, 4, 0.2f);
			ratings.Add(2, 2, 0.6f);
			ratings.Add(2, 5, 0.4f);
			ratings.Add(3, 7, 0.2f);
			ratings.Add(6, 3, 0.3f);

			Assert.AreEqual(0, ratings.CountByUser[0]);
			Assert.AreEqual(2, ratings.CountByUser[1]);
			Assert.AreEqual(3, ratings.CountByUser[2]);
			Assert.AreEqual(1, ratings.CountByUser[3]);
			Assert.AreEqual(0, ratings.CountByUser[4]);
			Assert.AreEqual(0, ratings.CountByUser[5]);
			Assert.AreEqual(1, ratings.CountByUser[6]);

			Assert.AreEqual(0, ratings.CountByItem[0]);
			Assert.AreEqual(0, ratings.CountByItem[1]);
			Assert.AreEqual(1, ratings.CountByItem[2]);
			Assert.AreEqual(1, ratings.CountByItem[3]);
			Assert.AreEqual(2, ratings.CountByItem[4]);
			Assert.AreEqual(1, ratings.CountByItem[5]);
			Assert.AreEqual(0, ratings.CountByItem[6]);
			Assert.AreEqual(1, ratings.CountByItem[7]);
			Assert.AreEqual(1, ratings.CountByItem[8]);
		}
	}
}