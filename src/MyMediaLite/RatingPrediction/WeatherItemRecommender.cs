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
using System.IO;
using System.Collections.Generic;
using MyMediaLite.Data;
using MyMediaLite.DataType;
using MyMediaLite.IO;
using System.Linq;
/*! \namespace MyMediaLite.ItemRecommendation
 *  \brief This namespace contains item recommenders and some helper classes for item recommendation.
 */
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading.Tasks;


namespace MyMediaLite.RatingPrediction
{
	/// <summary>Weahter based enhancement of Rank-GeoFM</summary>
	/// <remarks>
	/// Alexander Oberegger
	/// [1] Rank-GeoFM: A Ranking based Geographical Factorization Method for Point of Interest 
	/// http://dl.acm.org/citation.cfm?id=2767722
	///
	/// </remarks>
	public class WeatherItemRecommender : TimeAwareRatingPredictor
	{



		public WeatherItemRecommender (double[,] U1,double[,] U2, double[,] U3, double[,] L1, Dictionary<int,int> idLocMapper, Dictionary<int,int> idUserMapper){
			this.U1 = new double[U1.GetLength (0), U1.GetLength (1)];
			alglib.rmatrixcopy (U1.GetLength (0), U1.GetLength (1), U1, 0, 0, ref this.U1, 0, 0);
			this.U2 = new double[U2.GetLength (0), U2.GetLength (1)];
			alglib.rmatrixcopy (U2.GetLength (0), U2.GetLength (1), U2, 0, 0, ref this.U2, 0, 0);
			this.U3 = new double[U3.GetLength (0), U3.GetLength (1)];
			alglib.rmatrixcopy (U3.GetLength (0), U3.GetLength (1), U3, 0, 0, ref this.U3, 0, 0);
			this.L1 = new double[L1.GetLength (0), L1.GetLength (1)];
			alglib.rmatrixcopy (L1.GetLength (0), L1.GetLength (1), L1, 0, 0, ref this.L1, 0, 0);
			this.idLocMapper = idLocMapper;
			this.idUserMapper = idUserMapper;

			parameters_initialized = true;
		}

		Dictionary<int,double[,]> sumWeatherFeatures;

		Dictionary<int, double[,]> sumGeos;

		/// <summary>
		/// Initializes a new instance of the <see cref="MyMediaLite.RatingPrediction.WeatherItemRecommender"/> class.
		/// </summary>
		private WeatherItemRecommender (){

		}

		/// <summary>
		/// Maps Database UserId to Matrix UserId in U1-U4
		/// </summary>
		Dictionary<int,int> idUserMapper;

		/// <summary>
		/// Maps Database LocationId to Matrix LocationId in L1
		/// </summary>
		Dictionary<int,int> idLocMapper;

		/// <summary>
		/// States if latent factors got initialized with constructor.
		/// </summary>
		public bool parameters_initialized = false;

		/// <summary>
		/// After evaluation_at iterations a statistic will be made.
		/// </summary>
		public int evaluation_at = 10;

		/// <summary>
		/// Maximum amount of iterations.
		/// </summary>
		public int max_iter = 1000;

		/// <summary>
		/// States if weather should be considered
		/// </summary>
		public bool weather_aware = false;

		/// <summary>
		/// States if weather category calculations should be made.
		/// </summary>
		public bool weather_category_aware = false;

		/// <summary>
		/// Hyperparameter ε initialized as in [1]
		/// </summary>
		public double epsilon = 0.3;

		/// <summary>
		/// Hyperparameter C initialized as in [1]
		/// </summary>
		public double C = 1.0;

		/// <summary>
		/// Hyperparameter Learning rate γ initialized as in [1]
		/// </summary>
		public double gamma = 0.0001;

		/// <summary>
		/// "As a result, tuning the hyperparameter α can balance the contributions of user-preference and geographical influence scores to the final recommendation score." [1]
		/// "We find that Rank-GeoFM perfoms the best at α = 0.2 for POI recommendation on both data, and performs the best at α=0.1 for time-aware POI recommendation on both data. " [1]
		/// </summary>
		public double alpha = 0.2;

		/// <summary>
		/// "As a result, tuning the hyperparameter beta can balance the contributions of user-preference and weather influence scores to the final recommendation score." [1]
		/// </summary>
		public double beta = 0.2;

		/// <summary>
		/// "As a result, tuning the hyperparameter mu can balance the contributions of user-preference and weather category influence scores to the final recommendation score." [1]
		/// </summary>
		public double mu = 0.2;

		/// <summary>
		/// Nk(ℓ) the set of k nearest POIs of ℓ
		/// </summary>
		public int k = 300;

		/// <summary>
		/// K dimension for model paramaters Θ
		/// </summary>
		public int K = 100;

		/// <summary>
		/// Stores the distances between locations
		/// </summary>
		private SparseMatrix<double> distanceMatrix;

		/// <summary>
		/// Stores the distances between locations
		/// </summary>
		private SparseMatrix<double> categoryDistanceMatrix;

		/// <summary>
		/// W Matrix as mentioned in [1] W ∈ R |L|x|L|
		/// saving the geo probabilities between POI's
		/// </summary>
		private alglib.sparsematrix W;

		/// <summary>
		/// Climate Matrix CL ∈ R |L|x|L|
		/// saving the weather similarities/probabilities between POI's
		/// </summary>
		private alglib.sparsematrix CL;

		/// <summary>
		/// Climate Matrix CLC ∈ R |C|x|C|
		/// saving the weather similarities/probabilities between Categories
		/// </summary>
		private alglib.sparsematrix CLC;

		/// <summary>
		/// "Model Paramaeter U^(1) used to model the user's own preference. U(1) ∈ R|U|×K" [1]
		/// </summary>
		private double[,] U1;

		/// <summary>
		/// "Model Paramaeter L^(1) used to model the user's own preference. L(1) ∈ R|L|×K" [1]
		/// </summary>
		private double[,] L1;

		/// <summary>
		///  On the other hand, we introduce one extra latent factor matrix U(2) ∈ R|U|×K for users, and employ U(2) to model the interaction between users and
		///  POIs for incorporating the geographical influence.
		/// </summary>
		private double[,] U2;

		/// <summary>
		/// Extra latent factor matrix U(3) ∈ R|U|×K for users, and employ U(3) to model the interaction between users and 
		/// POIs for incorporating weather influence.
		/// </summary>
		private double[,] U3;


		/// <summary>
		/// Stores the nearest neighbors of each location.
		/// </summary>
		private Dictionary<int,IList<int>> nearestNeighbors;

		/// <summary>
		/// Stores the nearest neighbors of each location.
		/// </summary>
		private Dictionary<int,IList<int>> nearestNeighborsCategory;

		/// <summary>
		/// The venue category mapper.
		/// </summary>
		private Dictionary<int,int> venueCategoryMapper;

		/// <summary>
		/// Validation data
		/// </summary>
		/// <value>The validation.</value>
		public virtual ITimedRatings Validation
		{
			get { return validation_ratings; }
			set {
				validation_ratings = value;
				MaxUserID = ratings.MaxUserID;
				MaxItemID = ratings.MaxItemID;
				MinRating = ratings.Scale.Min;
				MaxRating = ratings.Scale.Max;
			}
		}

		/// <summary>validation data</summary>
		protected ITimedRatings validation_ratings;


		/// <summary>
		/// Validation data
		/// </summary>
		/// <value>The validation.</value>
		public virtual ITimedRatings Test
		{
			get { return test_ratings; }
			set {
				test_ratings = value;
				MaxUserID = ratings.MaxUserID;
				MaxItemID = ratings.MaxItemID;
				MinRating = ratings.Scale.Min;
				MaxRating = ratings.Scale.Max;
			}
		}

		protected ITimedRatings test_ratings;

		/// <summary>
		/// Predict rating or score for a given user-item combination
		/// </summary>
		/// <remarks></remarks>
		/// <param name="user_id">the user ID</param>
		/// <param name="item_id">the item ID</param>
		/// <returns>the predicted score/rating for the given user-item combination</returns>
		public override float Predict(int user_id, int item_id)
		{
			return (float)computeRecommendationScore (user_id, item_id);
		}

		/// <summary>
		/// predict rating at a certain point in time
		/// </summary>
		/// <param name="user_id">the user ID</param>
		/// <param name="item_id">the item ID</param>
		/// <param name="time">the time of the rating event</param>
		public override float Predict(int user_id, int item_id, DateTime time)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// This function converts decimal degrees to radians
		/// </summary>
		/// <param name="deg">Degrees</param>
		private double deg2rad(double deg) {
			return (deg * Math.PI / 180.0);
		}

		/// <summary>
		/// This function converts radians to decimal degrees
		/// </summary>
		/// <param name="rad">Radians</param>
		private double rad2deg(double rad) {
			return (rad / Math.PI * 180.0);
		}

		/// <summary>
		/// Distance in kilometer between two locations specified by lat1, lon1, lat2 and lon2.
		/// </summary>
		/// <param name="lat1">Latitude of location 1.</param>
		/// <param name="lon1">Longitude of location 1.</param>
		/// <param name="lat2">Latitude of location 2.</param>
		/// <param name="lon2">Longitude of location 2.</param>
		private double distance(double lat1, double lon1, double lat2, double lon2) {
			double theta = lon1 - lon2;
			double dist = Math.Sin (deg2rad (lat1)) * Math.Sin (deg2rad (lat2)) + Math.Cos (deg2rad (lat1)) * Math.Cos (deg2rad (lat2)) * Math.Cos (deg2rad (theta));
			dist = Math.Acos (dist);
			dist = rad2deg (dist);
			dist = dist * 60 * 1.1515;
			return dist * 1.609344;
		}

		/// <summary>
		/// Gets string for subselecting all category id's used from database
		/// </summary>
		/// <returns>The category in string.</returns>
		/// <param name="categories">Categories.</param>
		private string createCategoryInString (IList<int> categories){
			string all_ids = "(";
			bool first = true;
			foreach (int id in categories) {
				if (first) {
					all_ids += id.ToString ();
					first = false;
				} else
					all_ids += "," + id.ToString ();
			}
			all_ids += ")";
			return all_ids;

		}

		/// <summary>
		/// Gets string for subselecting all id's used from database
		/// </summary>
		/// <returns>The all identifiers string for database.</returns>
		private string getAllIdsStringForDatabase(){

			string all_ids = "(";
			bool first = true;
			foreach (int id in ratings.AllItems) {
				if (first) {
					all_ids += id.ToString ();
					first = false;
				} else
					all_ids += "," + id.ToString ();
			}
			all_ids += ")";
			return all_ids;
		}

		/// <summary>
		/// Initializes the distance matrix between all POI's
		/// </summary>
		private void createDistanceMatrix(){
			DBConnect conn = new DBConnect (connection_string);
			string all_ids = getAllIdsStringForDatabase ();
			List<string>[] res = conn.Select("select id_int, v.lat, v.lon from VENUE v " +
				" where id_int in "+all_ids + " order by id_int ",3);
			distanceMatrix = new SparseMatrix<double>(res[0].Count, res[0].Count);

			List<string> id_ints = res [0];
			List<string> lats = res [1];
			List<string> lons = res [2];
			int i = 0;
			foreach(string id1 in id_ints){
				int j = 0;
				int x = int.Parse(id1);
				int y = 0;
				foreach(string id2 in id_ints){
					y = int.Parse (id2);
					double dist = distance (double.Parse(lats[i]), double.Parse(lons[i]), double.Parse(lats[j]), double.Parse(lons[j]));
					if (Double.IsNaN (dist) || x == y) {
						distanceMatrix [x, y] = 100000000;

					} else {
						distanceMatrix [x, y] = distance (double.Parse (lats [i]), double.Parse (lons [i]), double.Parse (lats [j]), double.Parse (lons [j]));
					}
					j++;
				}
				i++;
			}
		}

		private double getDistanceProbabilityCategory(int c1, int c2){
			return categoryDistanceMatrix [c1, c2];
		}

		/// <summary>
		/// Get the probability of l' being checked-in when l has been checked-in regarding to [1]
		/// </summary>
		/// <returns>The distance probability.</returns>
		/// <param name="l1">l</param>
		/// <param name="l2">l'</param>
		private double getDistanceProbability(int l1, int l2){
			return 1/(0.5 + distanceMatrix[l1, l2]);
		}

		/// <summary>
		/// Returns the k nearest neighbors (N_k(l) in [1]) of location l
		/// </summary>
		/// <returns>The nearest neighbors.</returns>
		/// <param name="l">L.</param>
		private Dictionary<int,double> getNearestNeighbors(int l){
			return distanceMatrix[l].OrderBy(entry=>entry.Value).Take(k).ToDictionary(pair=>pair.Key,pair=>pair.Value);
		}

		/// <summary>
		/// Initializes the nearest neighbor matrix.
		/// </summary>
		private void initNearestNeighborMatrix (){
			nearestNeighbors = new Dictionary<int,IList<int>> ();
			foreach (int id in ratings.AllItems) {
				nearestNeighbors[id] = getNearestNeighbors (id).Keys.ToList ();
			}
		}

		/// <summary>
		/// Creates the W matrix that contains probabilities that user visits POI l when l' has been visited.
		/// </summary>
		private void createWMatrix(){
			alglib.sparsecreate (ratings.AllItems.Max()+1, ratings.AllItems.Max()+1, out this.W);
			foreach(int id1 in ratings.AllItems){
				try{
					if (nearestNeighbors[id1].Count < k){
						Console.WriteLine("Unter 100!!");
						throw new Exception();
					}
					foreach(int id2 in nearestNeighbors[id1]){
						if(id1 == id2){
							continue;
						}
						alglib.sparseset (this.W, id1, id2, getDistanceProbability(id1, id2));
					}

				}catch(Exception e){
					
				}

			}
			alglib.sparseconverttocrs(W);

			W.normalize (ratings.AllItems,k);
		}

		private void createWeatherCategoryDistance(){
			DBConnect conn = new DBConnect (connection_string);
			string all_ids = getAllIdsStringForDatabase ();
			List<string>[] res = conn.Select("select ca.id, v.id_int from VENUE v inner join CATEGORY ca on(v.category_id = ca.id) where v.id_int in " + all_ids ,2);
			List<string> ca_ids = res[0];
			List<string> venue_ids = res[1];
			venueCategoryMapper = new Dictionary<int,int> ();
			int i = 0;
			foreach (string venue_id in venue_ids) {
				int v_id = int.Parse (venue_id);
				int ca_id = int.Parse(ca_ids[i]);
				venueCategoryMapper.Add (v_id, ca_id);
				i++;
			}
			all_ids = createCategoryInString (venueCategoryMapper.Values.Distinct ().ToList ());
			res = conn.Select ("select * from weather_avgs_per_category ca " +
			" where ca.id in " + all_ids + " order by ca.id ", 9);

			ca_ids = res [0];
			List<string> temperature = res [1];
			List<string> precip_intensity = res [2];
			List<string> wind_speed = res [3];
			List<string> humidity = res [4];
			List<string> cloud_cover = res [5];
			List<string> pressure = res [6];
			List<string> visibility = res [7];
			categoryDistanceMatrix = new SparseMatrix<double>(ca_ids.Count, ca_ids.Count);
			i = 0;
			foreach (string id1 in ca_ids) {
				int j = 0;
				int x = int.Parse (id1);
				int y = 0;
				foreach (string id2 in ca_ids) {
					y = int.Parse (id2);
					if (x == y) {
						continue;
					}
					IList<double> vector1 = new List<double> { double.Parse(temperature [i]), double.Parse(precip_intensity [i]), double.Parse(wind_speed [i]), double.Parse(humidity [i]),
						double.Parse(cloud_cover [i])}; // ,  double.Parse(visibility [i]), double.Parse(pressure [i]),
					IList<double> vector2 = new List<double> { double.Parse(temperature [j]), double.Parse(precip_intensity [j]), double.Parse(wind_speed [j]), double.Parse(humidity [j]),
						double.Parse(cloud_cover [j])}; // ,  double.Parse(visibility [j]), double.Parse(pressure [j]),
					categoryDistanceMatrix[x,y] = vector1.CosineSimilarity(vector2);
					j++;
				}
				i++;
			}
		}

		/// <summary>
		/// creates the weather similarity matrix CL
		/// </summary>
		private void createClimateMatrix(){		
			DBConnect conn = new DBConnect (connection_string);
			alglib.sparsecreate (ratings.AllItems.Max()+1, ratings.AllItems.Max()+1, out this.CL);
			string all_ids = getAllIdsStringForDatabase ();
			List<string>[] res = conn.Select("select * from weather_avgs_per_venue wa " +
				" where wa.id_int in " + all_ids + " order by wa.id_int " ,9);

			List<string> id_ints = res [0];
			List<string> temperature = res [1];
			List<string> precip_intensity = res [2];
			List<string> wind_speed = res [3];
			List<string> humidity = res [4];
			List<string> cloud_cover = res [5];
			List<string> pressure = res [6];
			List<string> visibility = res [7];
			List<string> moonphase = res [8];

			int i = 0;
			foreach (string id1 in id_ints) {
				int j = 0;
				int x = int.Parse (id1);

				foreach(int y in nearestNeighbors[x]){
					if(x == y){
						continue;
					}
					IList<double> vector1 = new List<double> {
						double.Parse (temperature [i]),
						double.Parse (precip_intensity [i]),
						double.Parse (wind_speed [i]),
						double.Parse (humidity [i]),
						double.Parse (cloud_cover [i]),
						double.Parse (pressure [i]),
						double.Parse (visibility [i]),
						double.Parse (moonphase [i])
					};
					IList<double> vector2 = new List<double> {
						double.Parse (temperature [j]),
						double.Parse (precip_intensity [j]),
						double.Parse (wind_speed [j]),
						double.Parse (humidity [j]),
						double.Parse (cloud_cover [j]),
						double.Parse (pressure [j]),
						double.Parse (visibility [j]),
						double.Parse (moonphase [j])
					};
					alglib.sparseset (this.CL, x, y, vector1.CosineSimilarity(vector2));
					j++;
				}
				i++;
			}
			alglib.sparseconverttocrs(CL);
			CL.normalize (ratings.AllItems,k);
		}
			
		private IList<int> shuffleList(IList<int> list){
			list.Shuffle();
			return list;
		}

		/// <summary>
		/// Calculates sum(cl_{l,l*}*l_{l*}^(1)
		/// </summary>
		/// <returns>The geographical weight sum of a location.</returns>
		/// <param name="l">Location.</param>
		private double[,] sumWeightOfTwoLocationsWeather (int l){
			double [,] sum = new double[K,1];
			double[] wrow = new double[0];
			int[] colids = new int[0] ;
			int tmp;
//			double[,] cl_l1l2 = new double[1, 1];
			alglib.sparsegetcompressedrow(CL,l, ref colids, ref wrow, out tmp);
			int[] mappedL2 = idLocMapper.GetValues (colids);
			int j = 0;
			foreach (double scalar in wrow){
				for (int i = 0; i < K; i++) {
					sum [i, 0] += L1 [mappedL2 [j], i] * scalar;
				}
				j++;
			}
			return sum;
		}

		/// <summary>
		/// Calculates sum(w_{l,l*}*l_{l*}^(1)
		/// </summary>
		/// <returns>The geographical weight sum of a location.</returns>
		/// <param name="l">Location.</param>
		private double[,] sumWeightOfTwoLocationsGeo (int l){
			double [,] sum = new double[K,1];
			double[] wrow = new double[0];
			int[] colids = new int[0] ;
			int tmp;
			alglib.sparsegetcompressedrow(W,l, ref colids, ref wrow, out tmp); // wrow --> k length  double[,] wll* = new double[K,k]   wll*[i] = wrow
			int[] mappedL2 = idLocMapper.GetValues (colids);
			int j = 0;
			foreach (double scalar in wrow){
				for (int i = 0; i < K; i++) {
					sum [i, 0] += L1 [mappedL2 [j], i] * scalar;
				}
				j++;
			}

			return sum;
		}
			

		/// <summary>
		/// Computes the recommendation score. equation 4 of [1]
		/// </summary>
		/// <returns>The recommendation score.</returns>
		/// <param name="u">U.</param>
		/// <param name="l">L.</param>
		private double computeRecommendationScore(int u, int l){
			double[,] C = new double[1, 1];
			try{
				double[,] sumGeo = sumGeos[l];
				alglib.rmatrixgemm (1, 1, K, 1,L1, idLocMapper [l], 0, 0, U1, idUserMapper [u], 0, 1, 0,ref C, 0, 0);
				alglib.rmatrixgemm (1, 1, K, 1,sumGeo, 0, 0, 1, U2, idUserMapper [u], 0, 1, 1, ref C, 0, 0);
				if (weather_aware) {
					double[,] sumWeather = sumWeatherFeatures[l];
					alglib.rmatrixgemm (1, 1, K, 1,sumWeather, 0, 0, 1, U3, idUserMapper [u], 0, 1, 1, ref C, 0, 0);
				}
			}catch(alglib.alglibexception e){
				Console.WriteLine (e.msg);
			}
			return C [0, 0];
		}

		/// <summary>
		/// Indicator function returns 1 if statement is true 0 otherwise
		/// </summary>
		/// <returns>The function.</returns>
		/// <param name="statement">If set to <c>true</c> statement.</param>
		private int indicatorFunction(bool statement){
			if (statement)
				return 1;
			else
				return 0;
		}

		/// <summary>
		/// Rating incompatibility function.
		/// </summary>
		/// <returns>The function.</returns>
		/// <param name="xul">Xul.</param>
		/// <param name="xul2">Xul2.</param>
		/// <param name="yul">Yul.</param>
		/// <param name="yul2">Yul2.</param>
		private int incompFunction(double xul,double xul2,double yul,double yul2){
			return indicatorFunction (xul > xul2) * indicatorFunction (yul < (yul2 + epsilon));
		}

		/// <summary>
		/// Used to approximate the indicator funciton. [1]
		/// </summary>
		/// <returns>The function.</returns>
		/// <param name="a">The alpha component.</param>
		private double sigmoidFunction(double a){
			return (1 / (1 + Math.Exp (-a)));
		}

		/// <summary>
		/// Function for computing δuℓℓ′ [1].
		/// </summary>
		/// <returns>The function.</returns>
		/// <param name="y_ul">Y ul.</param>
		/// <param name="y_ul2">Y ul2.</param>
		private double deltaFunction(double y_ul, double y_ul2){
			return (sigmoidFunction (y_ul2 + epsilon - y_ul) * (1 - (sigmoidFunction (y_ul2 + epsilon - y_ul))));
		}

		/// <summary>
		/// Computs the ranking incompatibility from incompFunction into a loss [1]
		/// </summary>
		/// <param name="r">Rating incompatibility.</param>
		private double E(int r){
			double sum = 0;
			for (int i = 1; i <= r; i++) {
				sum += 1 / i;
			}
			return sum;
		}


		/// <summary>
		/// Normalizes a vectors euclidean norm to C.
		/// </summary>
		/// <returns>The to c.</returns>
		/// <param name="orig">Original.</param>
		/// <param name="value">The value to normalize to.</param> 
		private IList<double> normalizeEuclidean(IList<double> orig, double value){
			double euclidNorm = (double)orig.EuclideanNorm ();
			if (euclidNorm > value) {
				IList<double> tmp = orig.Divide (euclidNorm).Multiply (value);
				return tmp;
			}
			return orig;
		}
			

		private void init(){
			Console.WriteLine("epsilon = "+epsilon.ToString()+"; C = "+C.ToString()+"; gamma = "+gamma.ToString()+"; alpha = "+alpha.ToString()+"; k = "+k.ToString()+"; K = "+K.ToString()+"; WeatherAware = "+weather_aware.ToString()+"; beta = "+beta.ToString()+"; WeatherAwareCategory = "+weather_category_aware.ToString()+"; mu = "+ mu.ToString());
			Console.WriteLine ("Start create distance Matrix at:");
			Console.WriteLine (DateTime.Now);
			createDistanceMatrix ();
			Console.WriteLine ("End create distance Matrix");
			Console.WriteLine ("Start create NearestNeighbor Matrix at:");
			Console.WriteLine (DateTime.Now);
			initNearestNeighborMatrix ();
			Console.WriteLine ("end create NearestNeighbor Matrix");
			Console.WriteLine ("Start create W Matrix at:");
			Console.WriteLine (DateTime.Now);
			createWMatrix ();
			Console.WriteLine ("End create W Matrix");
			//if (weather_aware) {
			Console.WriteLine ("Start create Climate Matrix at:");
			Console.WriteLine (DateTime.Now);
			createClimateMatrix ();
			Console.WriteLine ("End create Climate Matrix");
			Console.WriteLine ("Finished Init at:");
			Console.WriteLine (DateTime.Now);
		}

		/// <summary>
		/// Adjusts the weather aware latent factors between locations.
		/// </summary>
		/// <param name="l1">L1.</param>
		/// <param name="l2">L2.</param>
		/// <param name="user">User.</param>
		/// <param name="eta">Eta.</param>
		public void adjustWeatherAwareBetweenLocations (int l1, int l2, int user, double eta){
			double[,] suml1 = sumWeatherFeatures [l1];
			double[,] suml2 = sumWeatherFeatures [l2];
			IList<double> g2 = new List<double> ();
			for (int i = 0; i < K; i++) {
				g2.Add(suml2 [i,0] - suml1 [i,0]);
			}

			user = idUserMapper [user];

			IList<double> values = new List<double> ();
			g2 = g2.Multiply (gamma * eta);
			for (int i = 0; i < K; i++){
				values .Add(U3 [user, i] - g2 [i]);
			}
			values = normalizeEuclidean (values, beta * C);
			for (int i = 0; i < K; i++) {
				U3 [user, i] = values [i];
			}
		}

		/// <summary>
		/// Adjusts geo latent factors according to [1].
		/// </summary>
		/// <param name="l1">L1.</param>
		/// <param name="l2">L2.</param>
		/// <param name="user">User.</param>
		/// <param name="eta">Eta.</param>
		public void adjustGeo (int l1, int l2, int user, double eta){
			IList<double> g = new List<double> ();
			double[,] suml2 = sumGeos [l2];
			double[,] suml1 = sumGeos [l1];
			for (int i = 0; i < K; i++) {
				g.Add(suml2 [i,0] - suml1 [i,0]);
			}
			user = idUserMapper [user];

			IList<double> values1 = new List<double> ();
			IList<double> values2 = new List<double> ();
			IList<double> values3 = new List<double> ();
			IList<double> values4 = new List<double> ();
			for (int i = 0; i < K; i++) {
				values1 .Add(U1 [user, i] - (gamma * eta * (L1 [idLocMapper [l2], i] - L1 [idLocMapper [l1], i])));
				values2 .Add(U2 [user, i] - (g[i]*gamma*eta));
				values3 .Add(L1[idLocMapper[l2],i]-(U1[user,i]*gamma*eta));
				values4 .Add(L1[idLocMapper[l1],i]+(U1[user,i]*gamma*eta));
					
			}
			values1 = normalizeEuclidean (values1, C);
			values2 = normalizeEuclidean (values2, alpha * C);
			values3 = normalizeEuclidean (values3, C);
			values4 = normalizeEuclidean (values4, C);
			for (int i = 0; i < K; i++) {
				U1 [user, i] = values1 [i];
				U2 [user, i] = values2 [i];
				L1 [idLocMapper [l2],i] = values3 [i];
				L1 [idLocMapper [l1],i] = values4 [i];
			}
		}


		/// <summary>
		/// Learn the model parameters of the recommender from the training data
		/// </summary>
		/// <remarks></remarks>
		public override void Train()
		{
			init ();

			Dictionary<int,IList<int>> user_items = timed_ratings.getItemsUserDict ();
			this.timed_ratings.AllUsers.Shuffle ();
			DateTime start_time = DateTime.Now;
			int i = 0;
			while (i <= max_iter) {

				sumGeos = new Dictionary<int,double[,]> ();
				foreach (int l in idLocMapper.Keys.ToList()) {
					sumGeos.Add(l, sumWeightOfTwoLocationsGeo (l));
				}

				sumWeatherFeatures = new Dictionary<int,double[,]> ();
				foreach (int l in idLocMapper.Keys.ToList()) {
					sumWeatherFeatures.Add(l, sumWeightOfTwoLocationsWeather(l));
				}

				Console.Write ("Start iteration: " + i.ToString () + " at: ");
				Console.WriteLine (DateTime.Now);
				ParallelOptions po = new ParallelOptions{
					MaxDegreeOfParallelism = Environment.ProcessorCount
				};
				IList<int> locations = shuffleList (this.timed_ratings.AllItems);
				Parallel.ForEach (this.timed_ratings.AllUsers, po, user => { 
					foreach (int l1 in user_items[user]) {
						int x_ul = timed_ratings.getCheckinCount (user, l1);
						if (x_ul == 0)
							throw new Exception ();
						int x_ul2 = 0;
						double y_ul2 = 0;
						double y_ul = 0;
						y_ul = computeRecommendationScore (user, l1);
						int n = 0;
						int l2 = 0;
						for (int innerl2 = 0; innerl2 < locations.Count; innerl2++) {
							if (locations [innerl2] == l1)
								continue;
							double innery_ul2 = 0;
							innery_ul2 = computeRecommendationScore (user, locations [innerl2]);
							n++;
							if (incompFunction (x_ul, x_ul2, y_ul, innery_ul2) == 1 && locations [innerl2] != l1) {
								y_ul2 = innery_ul2;
								l2 = locations [innerl2];
								x_ul2 = timed_ratings.getCheckinCount (user, l2);
								break;
							}
						}
						if (incompFunction (x_ul, x_ul2, y_ul, y_ul2) == 1) {
							double eta = E ((int)(this.ratings.AllItems.Count / n)) * deltaFunction (y_ul, y_ul2);
							adjustGeo (l1, l2, user, eta);
							if (weather_aware) {
								adjustWeatherAwareBetweenLocations (l1, l2, user, eta);
							}
						}

					}
				});
				Console.Write ("Finished iteration " + i.ToString ()+ " at: ");
				Console.WriteLine (DateTime.Now);
				if (i % evaluation_at == 0 && i != 0) {
					Console.WriteLine ("Start evaluation at iteration "+i.ToString());
					var results = Eval.ItemsWeatherItemRecommender.EvaluateTime (this, this.validation_ratings, this.timed_ratings, "VALIDATION " , false);
					results = Eval.ItemsWeatherItemRecommender.EvaluateTime (this, this.test_ratings, this.timed_ratings, "TEST " , false);
					Console.Write ("Finished evaluation after: ");
					Console.WriteLine (DateTime.Now);
				}
				i++;

			}
			Console.Write ("Finished training in: ");
			Console.WriteLine (((TimeSpan)(DateTime.Now - start_time)).TotalMilliseconds);
		}
	}
}