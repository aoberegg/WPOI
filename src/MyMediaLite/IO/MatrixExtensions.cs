// Copyright (C) 2010, 2011, 2012 Zeno Gantner
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
using System.Globalization;
using System.IO;
using System.Reflection;
using MyMediaLite.DataType;
using System.Collections.Generic;
using System.Linq;

namespace MyMediaLite.IO
{
	/// <summary>Utilities to work with matrices</summary>
	public static class MatrixExtensions
	{
		/// <summary>Write a matrix of floats to a StreamWriter object</summary>
		/// <param name="writer">a <see cref="StreamWriter"/></param>
		/// <param name="matrix">the matrix of floats to write out</param>
		static public void WriteMatrix(this TextWriter writer, IMatrix<float> matrix)
		{
			writer.WriteLine(matrix.NumberOfRows + " " + matrix.NumberOfColumns);
			for (int i = 0; i < matrix.NumberOfRows; i++)
				for (int j = 0; j < matrix.NumberOfColumns; j++)
					writer.WriteLine(i + " " + j + " " + matrix[i, j].ToString(CultureInfo.InvariantCulture));
			writer.WriteLine();
		}

		/// <summary>Write a sparse matrix of floats to a StreamWriter object</summary>
		/// <param name="writer">a <see cref="StreamWriter"/></param>
		/// <param name="matrix">the matrix of floats to write out</param>
		static public void WriteSparseMatrix(this TextWriter writer, SparseMatrix<float> matrix)
		{
			writer.WriteLine(matrix.NumberOfRows + " " + matrix.NumberOfColumns);
			foreach (var index_pair in matrix.NonEmptyEntryIDs)
				writer.WriteLine(index_pair.Item1 + " " + index_pair.Item2 + " " + matrix[index_pair.Item1, index_pair.Item2].ToString(CultureInfo.InvariantCulture));
			writer.WriteLine();
		}

		/// <summary>Write a sparse matrix of integers to a StreamWriter object</summary>
		/// <param name="writer">a <see cref="StreamWriter"/></param>
		/// <param name="matrix">the matrix of doubles to write out</param>
		static public void WriteSparseMatrix(this TextWriter writer, SparseMatrix<int> matrix)
		{
			writer.WriteLine(matrix.NumberOfRows + " " + matrix.NumberOfColumns);
			foreach (var index_pair in matrix.NonEmptyEntryIDs)
				writer.WriteLine(index_pair.Item1 + " " + index_pair.Item2 + " " + matrix[index_pair.Item1, index_pair.Item2].ToString());
			writer.WriteLine();
		}

		/// <summary>Read a matrix from a TextReader object</summary>
		/// <param name="reader">the <see cref="TextReader"/> object to read from</param>
		/// <param name="example_matrix">matrix of the type of matrix to create</param>
		/// <returns>a matrix of float</returns>
		static public IMatrix<float> ReadMatrix(this TextReader reader, IMatrix<float> example_matrix)
		{
			string[] numbers = reader.ReadLine().Split(' ');
			int dim1 = int.Parse(numbers[0]);
			int dim2 = int.Parse(numbers[1]);

			IMatrix<float> matrix = example_matrix.CreateMatrix(dim1, dim2);

			while ((numbers = reader.ReadLine().Split(' ')).Length == 3)
			{
				int i = int.Parse(numbers[0]);
				int j = int.Parse(numbers[1]);
				float v = float.Parse(numbers[2], CultureInfo.InvariantCulture);

				if (i >= dim1)
					throw new IOException("i = " + i + " >= " + dim1);
				if (j >= dim2)
					throw new IOException("j = " + j + " >= " + dim2);

				matrix[i, j] = v;
			}

			return matrix;
		}

		/// <summary>Read a matrix of integers from a TextReader object</summary>
		/// <param name="reader">the <see cref="TextReader"/> object to read from</param>
		/// <param name="example_matrix">matrix of the type of matrix to create</param>
		/// <returns>a matrix of integers</returns>
		static public IMatrix<int> ReadMatrix(this TextReader reader, IMatrix<int> example_matrix)
		{
			string[] numbers = reader.ReadLine().Split(' ');
			int dim1 = int.Parse(numbers[0]);
			int dim2 = int.Parse(numbers[1]);

			IMatrix<int> matrix = example_matrix.CreateMatrix(dim1, dim2);

			while ((numbers = reader.ReadLine().Split(' ')).Length == 3)
			{
				int i = int.Parse(numbers[0]);
				int j = int.Parse(numbers[1]);
				int v = int.Parse(numbers[2]);

				if (i >= dim1)
					throw new IOException("i = " + i + " >= " + dim1);
				if (j >= dim2)
					throw new IOException("j = " + j + " >= " + dim2);

				matrix[i, j] = v;
			}

			return matrix;
		}

		static public void normalize(this double[,] M, IList<int> ids){
			int i = 0;
			foreach (int id in ids) {
				int j = 0;
				double[] row = new double[M.GetLength (1)];
				foreach (int id2 in ids) {
					row[j] = M[id,id2];
					j++;
				}
				j = 0;
				double sum = row.Sum();
				row.Divide (sum);
				foreach (int id2 in ids) {
					M [id, id2] = row [j];
					j++;
				}

				i++;
			}

		}

		static public void normalize(this alglib.sparsematrix M, IList<int> ids, int k){
			foreach (int id in ids) {
				double[] wrow = new double[k];
				int[] colids = new int[k] ;
				int tmp;
				alglib.sparsegetcompressedrow(M, id, ref colids, ref wrow, out tmp);
				double sum = wrow.Sum ();
				IList<double> wrowl = wrow.ToList ();
				wrowl = wrowl.Divide (sum);
				int i = 0;
				foreach (int id2 in colids) {
					try{
						alglib.sparserewriteexisting (M, id, id2, wrowl [i]);
					}catch(alglib.alglibexception e){
						Console.WriteLine (e.msg);
					}
					i++;
				}
			}
		}

		static public void normalize(this Matrix<double> M, IList<int> ids){
			foreach(int id in ids){
				IList<double> row = M.GetRow(id);
				double sum = row.Sum();
				row.Divide (sum);
				M.SetRow (id, row);
			}
		}

		/// <summary>
		/// Normalize this instance.
		/// </summary>
		static public void normalize(this SparseMatrix<double> M, IList<int> ids){
			foreach(int id in ids){
				Dictionary<int,double> row = M[id];
				double sum = row.Sum(v => v.Value);
				foreach (int key in row.Keys.ToList()){
					row[key] = row[key] / sum;
				}
				M[id] = row;
			}
		}
	}
}