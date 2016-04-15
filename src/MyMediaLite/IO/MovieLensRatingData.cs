// Copyright (C) 2010, 2011, 2012, 2013 Zeno Gantner
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
using System.Data;
using System.Globalization;
using System.IO;
using MyMediaLite.Data;

namespace MyMediaLite.IO
{
	/// <summary>Class that offers static methods for reading in MovieLens 1M and 10M rating data</summary>
	/// <remarks>
	/// See http://www.grouplens.org/node/73#attachments and http://recsyswiki.com/wiki/MovieLens
	/// </remarks>
	public static class MovieLensRatingData
	{
		/// <summary>Read in rating data from a file</summary>
		/// <param name="filename">the name of the file to read from</param>
		/// <param name="user_mapping">mapping object for user IDs</param>
		/// <param name="item_mapping">mapping object for item IDs</param>
		/// <param name="test_rating_format">whether there is a rating column in each line or not</param>
		/// <returns>the rating data</returns>
		static public ITimedRatings Read(
			string filename,
			IMapping user_mapping = null, IMapping item_mapping = null,
			TestRatingFileFormat test_rating_format = TestRatingFileFormat.WITH_RATINGS)
		{
			string binary_filename = filename + ".bin.TimedRatings";
			if (FileSerializer.Should(user_mapping, item_mapping) && File.Exists(binary_filename))
				return (ITimedRatings) FileSerializer.Deserialize(binary_filename);

			return Wrap.FormatException<ITimedRatings>(filename, delegate() {
				using ( var reader = new StreamReader(filename) )
				{
					var ratings = (TimedRatings) Read(reader, user_mapping, item_mapping, test_rating_format);
					if (FileSerializer.Should(user_mapping, item_mapping) && FileSerializer.CanWrite(binary_filename))
						ratings.Serialize(binary_filename);
					return ratings;
				}
			});
		}

		/// <summary>Read in rating data from a TextReader</summary>
		/// <param name="reader">the <see cref="TextReader"/> to read from</param>
		/// <param name="user_mapping">mapping object for user IDs</param>
		/// <param name="item_mapping">mapping object for item IDs</param>
		/// <param name="test_rating_format">whether there is a rating column in each line or not</param>
		/// <returns>the rating data</returns>
		static public ITimedRatings Read(
			TextReader reader,
			IMapping user_mapping = null, IMapping item_mapping = null,
			TestRatingFileFormat test_rating_format = TestRatingFileFormat.WITH_RATINGS)
		{
			if (user_mapping == null)
				user_mapping = new IdentityMapping();
			if (item_mapping == null)
				item_mapping = new IdentityMapping();

			var ratings = new TimedRatings();

			string[] separators = { "::" };
			string line;
			int seconds_pos = test_rating_format == TestRatingFileFormat.WITH_RATINGS ? 3 : 2;

			while ((line = reader.ReadLine()) != null)
			{
				string[] tokens = line.Split(separators, StringSplitOptions.None);

				if (test_rating_format == TestRatingFileFormat.WITH_RATINGS && tokens.Length < 4)
					throw new FormatException("Expected at least 4 columns: " + line);
				if (test_rating_format == TestRatingFileFormat.WITHOUT_RATINGS && tokens.Length < 3)
					throw new FormatException("Expected at least 3 columns: " + line);

				int user_id = user_mapping.ToInternalID(tokens[0]);
				int item_id = item_mapping.ToInternalID(tokens[1]);
				float rating = test_rating_format == TestRatingFileFormat.WITH_RATINGS ? float.Parse(tokens[2], CultureInfo.InvariantCulture) : 0;
				long seconds = uint.Parse(tokens[seconds_pos]);

				var time = new DateTime(seconds * 10000000L).AddYears(1969);
				var offset = TimeZone.CurrentTimeZone.GetUtcOffset(time);
				time -= offset;

				ratings.Add(user_id, item_id, rating, time);
			}
			return ratings;
		}
	}
}