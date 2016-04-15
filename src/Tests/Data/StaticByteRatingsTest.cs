// Copyright (C) 2011, 2012 Zeno Gantner
// Copyright (C) 2010 Christina Lichtenthäler
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
	public class StaticByteRatingsTest
	{
		[Test()] public void TestMinRatingMaxRating()
		{
			var ratings = new StaticByteRatings(7);
			ratings.Add(1, 4, 3);
			ratings.Add(1, 8, 2);
			ratings.Add(2, 4, 2);
			ratings.Add(2, 2, 6);
			ratings.Add(2, 5, 4);
			ratings.Add(3, 7, 2);
			ratings.Add(6, 3, 3);

			Assert.AreEqual(6, ratings.Scale.Max);
			Assert.AreEqual(2, ratings.Scale.Min);
		}

		[Test()]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void TestFull()
		{
			var ratings = new StaticByteRatings(2);
			Assert.AreEqual(0, ratings.Count);
			ratings.Add(1, 4, 3);
			Assert.AreEqual(1, ratings.Count);
			ratings.Add(1, 8, 2);
			Assert.AreEqual(2, ratings.Count);
			ratings.Add(2, 4, 2);
		}

		[Test()] public void TestMaxUserIDMaxItemID()
		{
			var ratings = new StaticByteRatings(7);
			ratings.Add(1, 4, 3);
			ratings.Add(1, 8, 2);
			ratings.Add(2, 4, 2);
			ratings.Add(2, 2, 6);
			ratings.Add(2, 5, 4);
			ratings.Add(3, 7, 2);
			ratings.Add(6, 3, 3);

			Assert.AreEqual(6, ratings.MaxUserID);
			Assert.AreEqual(8, ratings.MaxItemID);
		}

		[Test()] public void TestAdd()
		{
			var ratings = new StaticByteRatings(7);
			ratings.Add(1, 4, 3);
			Assert.AreEqual(1, ratings.Count);
			ratings.Add(1, 8, 2);
			Assert.AreEqual(2, ratings.Count);
			ratings.Add(2, 4, 2);
			ratings.Add(2, 2, 6);
			ratings.Add(2, 5, 4);
			ratings.Add(3, 7, 2);
			ratings.Add(6, 3, 3);

			Assert.AreEqual(4, ratings[2, 5]);
			Assert.AreEqual(3, ratings[1, 4]);
			Assert.AreEqual(3, ratings[6, 3]);
			Assert.AreEqual(7, ratings.Count);
		}

		[Test()]
		[ExpectedException(typeof(NotSupportedException))]
		public void TestRemoveAt()
		{
			var ratings = new StaticByteRatings(8);
			ratings.Add(1, 4, 3);
			ratings.Add(1, 8, 2);
			ratings.Add(2, 4, 2);
			ratings.Add(2, 2, 6);
			ratings.Add(2, 5, 4);
			ratings.Add(3, 7, 2);
			ratings.Add(3, 3, 3);
			ratings.Add(6, 3, 3);

			Assert.AreEqual(8, ratings.Count);
			ratings.RemoveAt(ratings.GetIndex(2, 5));
		}

		[Test()]
		[ExpectedException(typeof(NotSupportedException))]
		public void TestRemoveUser()
		{
			var ratings = new StaticByteRatings(7);
			ratings.Add(1, 4, 3);
			ratings.Add(1, 8, 2);
			ratings.Add(2, 4, 2);
			ratings.Add(2, 2, 6);
			ratings.Add(2, 5, 4);
			ratings.Add(3, 7, 2);
			ratings.Add(3, 3, 3);

			Assert.AreEqual(7, ratings.Count);
			ratings.RemoveUser(2);
		}

		[Test()]
		[ExpectedException(typeof(NotSupportedException))]
		public void TestRemoveItem()
		{
			var ratings = new StaticByteRatings(7);
			ratings.Add(1, 4, 3);
			ratings.Add(1, 8, 2);
			ratings.Add(2, 4, 2);
			ratings.Add(2, 2, 6);
			ratings.Add(2, 5, 4);
			ratings.Add(3, 4, 2);
			ratings.Add(3, 3, 3);

			Assert.AreEqual(7, ratings.Count);
			ratings.RemoveItem(4);
		}

		[Test()] public void TestIndex()
		{
			var ratings = new StaticByteRatings(8);
			ratings.Add(1, 4, 3);
			ratings.Add(1, 8, 2);
			ratings.Add(2, 4, 2);
			ratings.Add(2, 2, 6);
			ratings.Add(2, 5, 4);
			ratings.Add(3, 4, 2);
			ratings.Add(3, 3, 3);
			ratings.Add(6, 3, 3);

			// test index[,]
			Assert.AreEqual(3, ratings[1, 4]);
			Assert.AreEqual(2, ratings[1, 8]);
			Assert.AreEqual(6, ratings[2, 2]);
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