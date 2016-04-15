// Copyright (C) 2011, 2012 Zeno Gantner
// Copyright (C) 2010 Steffen Rendle, Zeno Gantner
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

namespace MyMediaLite.DataType
{
	/// <summary>Sparse representation of a boolean matrix, using HashSets</summary>
	/// <remarks>
	/// Fast row-wise access is possible.
	/// Indexes are zero-based.
	/// </remarks>
	public class SparseBooleanMatrix : IBooleanMatrix
	{
		/// <summary>internal data representation: list of sets representing the rows</summary>
		protected internal IList<ISet<int>> row_list = new List<ISet<int>>();

		/// <summary>Indexer to access the elements of the matrix</summary>
		/// <param name="x">the row ID</param>
		/// <param name="y">the column ID</param>
		public bool this [int x, int y]
		{
			get {
				if (x < row_list.Count)
					return row_list[x].Contains(y);
				else
					return false;
			}
			set {
				if (value)
				{
					if (this[x] == null)
						throw new Exception("<<<" + x + ">>>");
					this[x].Add(y);
				}
				else
					this[x].Remove(y);
			}
		}

		///
		public ICollection<int> this [int x]
		{
			get {
				if (x >= row_list.Count)
					for (int i = row_list.Count; i <= x; i++)
						row_list.Add(new HashSet<int>());

				return row_list[x];
			}
		}

		///
		public virtual bool IsSymmetric
		{
			get {
				for (int i = 0; i < row_list.Count; i++)
					foreach (var j in row_list[i])
						if (!this[j, i])
							return false;
				return true;
			}
		}

		///
		public IMatrix<bool> CreateMatrix(int x, int y)
		{
			return new SparseBooleanMatrix();
		}

		///
		public IList<int> GetEntriesByRow(int row_id)
		{
			return row_list[row_id].ToList();
		}

		///
		public int NumEntriesByRow(int row_id)
		{
			return row_list[row_id].Count;
		}

		/// <remarks>Takes O(N) worst-case time, where N is the number of rows, if the internal hash table can be queried in constant time.</remarks>
		public IList<int> GetEntriesByColumn(int column_id)
		{
			var list = new List<int>();

			for (int row_id = 0; row_id < NumberOfRows; row_id++)
				if (row_list[row_id].Contains(column_id))
					list.Add(row_id);
			return list;
		}

		///
		public int NumEntriesByColumn(int column_id)
		{
			int count = 0;

			for (int row_id = 0; row_id < NumberOfRows; row_id++)
				if (row_list[row_id].Contains(column_id))
					count++;
			return count;
		}

		///
		public IList<int> NonEmptyRowIDs
		{
			get	{
				var row_ids = new List<int>();

				for (int i = 0; i < row_list.Count; i++)
					if (row_list[i].Count > 0)
						row_ids.Add(i);

				return row_ids;
			}
		}

		///
		/// <remarks>iterates over the complete data structure</remarks>
		public IList<int> NonEmptyColumnIDs
		{
			get {
				var col_ids = new HashSet<int>();

				// iterate over the complete data structure to find column IDs
				for (int i = 0; i < row_list.Count; i++)
					foreach (int id in row_list[i])
						col_ids.Add(id);

				return col_ids.ToArray();
			}
		}

		/// <summary>The number of rows in the matrix</summary>
		/// <value>The number of rows in the matrix</value>
		public int NumberOfRows	{ get { return row_list.Count; } }

		/// <summary>The number of columns in the matrix</summary>
		/// <value>The number of columns in the matrix</value>
		public int NumberOfColumns {
			get {
				int max_column_id = -1;
				foreach (var row in row_list)
					if (row.Count > 0)
						max_column_id = Math.Max(max_column_id, row.Max());

				return max_column_id + 1;
			}
		}

		///
		public int NumberOfEntries
		{
			get {
				int n = 0;
				foreach (var row in row_list)
					n += row.Count;
				return n;
			}
		}

		///
		public void Resize(int num_rows, int num_cols)
		{
			// if necessary, grow rows
			if (num_rows > NumberOfRows)
				for (int i = row_list.Count; i < num_rows; i++)
					row_list.Add( new HashSet<int>() );
			// if necessary, shrink rows
			if (num_rows < NumberOfRows)
				for (int i = NumberOfRows - 1; i >= num_rows; i--)
					row_list.RemoveAt(i);

			// if necessary, shrink columns
			if (num_cols < NumberOfColumns)
				foreach (var row in row_list)
					for (int i = NumberOfColumns - 1; i >= num_cols; i--)
						row.Remove(i);
		}

		/// <summary>Get the transpose of the matrix, i.e. a matrix where rows and columns are interchanged</summary>
		/// <returns>the transpose of the matrix (copy)</returns>
		public IMatrix<bool> Transpose()
		{
			var transpose = new SparseBooleanMatrix();
			for (int i = 0; i < row_list.Count; i++)
				foreach (int j in this[i])
					transpose[j, i] = true;
			return transpose;
		}

		/// <summary>Get the overlap of two matrices, i.e. the number of true entries where they agree</summary>
		/// <param name="s">the <see cref="SparseBooleanMatrix"/> to compare to</param>
		/// <returns>the number of entries that are true in both matrices</returns>
		public int Overlap(IBooleanMatrix s)
		{
			int c = 0;

			for (int i = 0; i < row_list.Count; i++)
				foreach (int j in row_list[i])
					if (s[i, j])
						c++;

			return c;
		}
	}
}