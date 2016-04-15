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
using System.Collections.Generic;
using MathNet.Numerics.Distributions;
using System.Linq;
using MathNet.Numerics;
using System.Threading.Tasks;

using System.Runtime.InteropServices; 

namespace MyMediaLite.DataType
{
	/// <summary>Extensions for vector-like data</summary>
	public static class VectorExtensions
	{
		static public double ScalarProductUnsafe(IList<double> v1, IList<double>v2){
			double ret = 0;
			unsafe {
				fixed (double* pV1 = v1.ToArray()) {
					fixed(double* pV2 = v2.ToArray()) {
						for (int i = 0; i < v1.Count; i++) {
							ret += *(pV1 + i) * *(pV2 +i);
						}
					}
				}
			}
			return ret;
		}

		[DllImport("calc.dll")] 
		public static extern void calculate (double[] v, ref double[] sum, double scalar, int K);

		/// <summary>Compute scalar product (dot product) of two vectors</summary>
		/// <returns>the scalar product of the arguments</returns>
		/// <param name='v1'>the first vector</param>
		/// <param name='v2'>the second vector</param>
		static public float ScalarProduct(IList<float> v1, IList<float> v2)
		{
			double result = 0;

			for (int i = 0; i < v1.Count; i++)
				result += v1[i] * v2[i];
			return (float) result;

		}

		/// <summary>Compute the Euclidean norm of a collection of floats</summary>
		/// <param name="vector">the vector to compute the norm for</param>
		/// <returns>the Euclidean norm of the vector</returns>
		static public double EuclideanNorm(this ICollection<double> vector)
		{
			double sum = 0;
			foreach (double v in vector)
				sum += Math.Pow(v, 2);
			return Math.Sqrt(sum);
		}

		/// <summary>Compute the Euclidean norm of a collection of floats</summary>
		/// <param name="vector">the vector to compute the norm for</param>
		/// <returns>the Euclidean norm of the vector</returns>
		static public float EuclideanNorm(this ICollection<float> vector)
		{
			float sum = 0;
			foreach (float v in vector)
				sum += (float)Math.Pow(v, 2);
			return (float)Math.Sqrt(sum);
		}

		/// <summary>Initialize a collection of floats with values from a normal distribution</summary>
		/// <param name="vector">the vector to initialize</param>
		/// <param name="mean">the mean of the normal distribution</param>
		/// <param name="stddev">the standard deviation of the normal distribution</param>
		static public void InitNormal(this IList<float> vector, double mean, double stddev)
		{
			var nd = new Normal(mean, stddev);
			nd.RandomSource = MyMediaLite.Random.GetInstance();

			for (int i = 0; i < vector.Count; i++)
				vector[i] = (float) nd.Sample();
		}

		/// <summary>Initialize a collection of floats with one value</summary>
		/// <param name="vector">the vector to initialize</param>
		/// <param name="val">the value to set each element to</param>
		static public void Init(this IList<float> vector, float val)
		{
			for (int i = 0; i < vector.Count; i++)
				vector[i] = val;
		}

		/// <summary>Add all entries of a vector to another vector</summary>
		/// <param name="vector">vector where vector2 is added to</param>
		/// <param name="vector2">vector2 is added to vector</param>
		static public void Add(this IList<float> vector, IList<float> vector2)
		{
			for (int i = 0; i < vector.Count; i++)
				vector[i] += vector2[i];
		}

		/// <summary>
		/// Adds the two vectors and returns the result.
		/// </summary>
		/// <returns>The with return.</returns>
		/// <param name="vector">Vector.</param>
		/// <param name="vector2">Vector2.</param>
		static public IList<float> AddWithReturn(this IList<float> vector, IList<float> vector2)
		{
			IList<float> result = new List<float>();
			for (int i = 0; i < vector.Count; i++)
				result.Add(vector[i] + vector2[i]);
			return result;
		}

		static public IList<float> MultiplyScalarAndSubtractFromList(this IList<float> vector, float scalar, IList<float> subtractFrom){
			for (int i = 0; i < vector.Count; i++) {
				subtractFrom[i] -= vector [i] * scalar; 
			}
			return subtractFrom;
		}
			
//		static public IList<float> MultiplyScalarAndAddToList(this IList<float> vector, float scalar, IList<float> addTo){
//			for (int i = 0; i < vector.Count; i++) {
//				addTo[i] += vector [i] * scalar; 
//			}
//			return addTo;
//		}

		static public void MultiplyScalarAndAddToList(this IList<float> vector, float scalar, ref float[] addTo){
			unsafe{
				fixed (float* pArray = vector.ToArray()){
					fixed(float* pArrayAdd = addTo){
						for (int i = 0; i < vector.Count; i++) {
							*(pArrayAdd+i) = *(pArray+i)*scalar;
						}
					}
				}
			}
//			for (int i = 0; i < vector.Count(); i++) {
//				addTo[i] += vector [i] * scalar; 
//			}
		}

//		static public void MultiplyScalarAndAddToList(this IList<float> vector, float scalar, ref IList<float> addTo){
//			for (int i = 0; i < vector.Count; i++) {
//				addTo[i] += vector [i] * scalar; 
//			}
//		}

		/// <summary>
		/// Multiply the specified vector and scalar.
		/// </summary>
		/// <param name="vector">Vector.</param>
		/// <param name="scalar">Scalar.</param>
		static public IList<double> Multiply(this IList<double> vector, double scalar)
		{
			IList<double> list_new  = new List<double> (vector);
			for (int i = 0; i < vector.Count; i++) {
					list_new [i] *= scalar;
			}
			return list_new;
//			for (int i = 0; i < vector.Count; i++) {
//				list_new[i] = vector [i] * scalar;
//			}
//			return list_new;
//			IList<float> list_new = new List<float> (vector);

//			return list_new;
		}

		/// <summary>
		/// Divide the specified vector and scalar.
		/// </summary>
		/// <param name="vector">Vector.</param>
		/// <param name="scalar">Scalar.</param>
		static public IList<double> Divide(this IList<double> vector, double scalar)
		{
			double[] list_new = new double[vector.Count];
			for (int i = 0; i < vector.Count; i++)
				list_new[i] = vector[i] / scalar;
			return list_new;
		}

		/// <summary>
		/// Print the specified vector.
		/// </summary>
		/// <param name="vector">Vector.</param>
		static public void print(this IList<float> vector){
			for (int j = 0; j < vector.Count; j++) {
				Console.Write (vector [j]+"," );
			}
			Console.WriteLine();
		}

		public static int [] GetValues(this Dictionary<int,int> dict, int[] keys){
			return keys.Intersect (dict.Keys).Select (k => dict [k]).ToArray();
		}

		/// <summary>
		/// Print the specified vector.
		/// </summary>
		/// <param name="vector">Vector.</param>
		static public void print(this IList<double> vector){
			for (int j = 0; j < vector.Count; j++) {
				Console.Write (vector [j]+"," );
			}
			Console.WriteLine();
		}

		/// <summary>
		/// Print the specified vector.
		/// </summary>
		/// <param name="vector">Vector.</param>
		static public void print(this IList<int> vector){
			for (int j = 0; j < vector.Count; j++) {
				Console.Write (vector [j]+"," );
			}
			Console.WriteLine();
		}

		/// <summary>
		/// Print the specified vector.
		/// </summary>
		/// <param name="vector">Vector.</param>
		static public void print(this IList<DateTime> vector){
			for (int j = 0; j < vector.Count; j++) {
				Console.Write (vector [j].ToString()+"," );
			}
			Console.WriteLine();
		}

		/// <summary>
		/// Minus the specified vector from vector2.
		/// </summary>
		/// <param name="vector">Vector.</param>
		/// <param name="vector2">Vector2.</param>
		static public void  Minus(this IList<double> vector, IList<double> vector2){
			if (vector.Count != vector2.Count)
				throw new DataMisalignedException ();

			for (int j = 0; j < vector.Count; j++) {
				vector [j] -= vector2 [j];
			}
		}

		/// <summary>
		/// Subtracts two vector and returns the result.
		/// </summary>
		/// <returns>The with return.</returns>
		/// <param name="vector">Vector.</param>
		/// <param name="vector2">Vector2.</param>
		static public IList<double> MinusWithReturn(this IList<double> vector, IList<double> vector2){
			IList<double> result = new List<double> ();
			for (int j = 0; j < vector.Count; j++) {
				result.Add(vector[j] - vector2 [j]);
			}
			return result;
		}

		static public double CosineSimilarity(this IList<double> vector, IList<double> vector2){
			int N = vector.Count;
			double dot = 0.0f;
			double mag1 = 0.0f;
			double mag2 = 0.0f;
			for (int n = 0; n < N; n++) {
				dot += vector [n] * vector2 [n];
				mag1 += (float)Math.Pow (vector [n], 2);
				mag2 += (float)Math.Pow (vector2 [n], 2);
			}
			return (float)(dot / (Math.Sqrt (mag1) * Math.Sqrt (mag2)));
		}
	}
}