
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
	public class WeatherContextAwareItemRecommender : TimeAwareRatingPredictor
	{



		public WeatherContextAwareItemRecommender (double[,] U1,double[,] U2, double[,] L1, double[,] L2, double[,] L3, double[,] F, Dictionary<int,int> idLocMapper, Dictionary<int,int> idUserMapper, int cityId, string weatherFeature){
			this.U1 = new double[U1.GetLength (0), U1.GetLength (1)];
			alglib.rmatrixcopy (U1.GetLength (0), U1.GetLength (1), U1, 0, 0, ref this.U1, 0, 0);
			this.U2 = new double[U2.GetLength (0), U2.GetLength (1)];
			alglib.rmatrixcopy (U2.GetLength (0), U2.GetLength (1), U2, 0, 0, ref this.U2, 0, 0);
			this.L1 = new double[L1.GetLength (0), L1.GetLength (1)];
			alglib.rmatrixcopy (L1.GetLength (0), L1.GetLength (1), L1, 0, 0, ref this.L1, 0, 0);
			this.L2 = new double[L2.GetLength (0), L2.GetLength (1)];
			alglib.rmatrixcopy (L2.GetLength (0), L2.GetLength (1), L2, 0, 0, ref this.L2, 0, 0);
			this.L3 = new double[L3.GetLength (0), L3.GetLength (1)];
			alglib.rmatrixcopy (L3.GetLength (0), L3.GetLength (1), L3, 0, 0, ref this.L3, 0, 0);
			this.F = new double[F.GetLength (0), F.GetLength (1)];
			alglib.rmatrixcopy (F.GetLength (0), F.GetLength (1), F, 0, 0, ref this.F, 0, 0);
			this.idLocMapper = idLocMapper;
			this.idUserMapper = idUserMapper;
			this.cityId = cityId;
			this.weatherFeature = weatherFeature;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MyMediaLite.RatingPrediction.WeatherItemRecommender"/> class.
		/// </summary>
		private WeatherContextAwareItemRecommender (){

		}

		Dictionary<int,double[,]> sumWeatherFeatures;

		Dictionary<int, double[,]> sumGeos;

		public int rangeSize = 20;

		double maxFeature;

		double minFeature;

		string weatherFeature;

		int cityId;

		/// <summary>
		/// Maps Database UserId to Matrix UserId in U1-U4
		/// </summary>
		Dictionary<int,int> idUserMapper;

		/// <summary>
		/// Maps Database LocationId to Matrix LocationId in L1
		/// </summary>
		Dictionary<int,int> idLocMapper;

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
		/// W Matrix as mentioned in [1] W ∈ R |L|x|L|
		/// saving the geo probabilities between POI's
		/// </summary>
		private alglib.sparsematrix W;

		/// <summary>
		/// Climate Matrix CL ∈ R |WeatherDimensions|x|K|
		/// saving the weather similarities/probabilities between POI's
		/// </summary>
		private double[,] F;

		/// <summary>
		/// "Model Paramaeter U^(1) used to model the user's own preference. U(1) ∈ R|U|×K" [1]
		/// </summary>
		private double[,] U1;

		/// <summary>
		///  On the other hand, we introduce one extra latent factor matrix U(2) ∈ R|U|×K for users, and employ U(2) to model the interaction between users and
		///  POIs for incorporating the geographical influence.
		/// </summary>
		private double[,] U2;

		/// <summary>
		/// "Model Paramaeter L^(1) used to model the user's own preference. L(1) ∈ R|L|×K" [1]
		/// </summary>
		private double[,] L1;

		/// <summary>
		/// "Model Paramaeter L^(2) used to model the user's own preference. L(2) ∈ R|L|×K" [1]
		/// </summary>
		private double[,] L2;

		/// <summary>
		/// "Model Paramaeter L^(3) used to model the user's own preference. L(3) ∈ R|L|×K" [1]
		/// </summary>
		private double[,] L3;


		/// <summary>
		/// Stores the nearest neighbors of each location.
		/// </summary>
		private Dictionary<int,IList<int>> nearestNeighbors;

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
		/// test data
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
			return (float)computeRecommendationScoreFeature (user_id, item_id, time);
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
		/// Distance in kilometer between two locations specified by lat1, lon1, lat2 and lon2. Look: https://www.geodatasource.com/developers/c-sharp
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
			alglib.sparsecreate (idLocMapper.Keys.Max ()+1, idLocMapper.Keys.Max ()+1, out this.W);
			foreach(int id1 in ratings.AllItems){
				try{
					if (nearestNeighbors[id1].Count < k){
						Console.WriteLine("Unter 100!!");
						throw new Exception();
					}
					foreach(int id2 in nearestNeighbors[id1]){
						if(id1 == id2){
							throw new Exception();
						}
						alglib.sparseset (this.W, id1, id2, getDistanceProbability(id1, id2));
					}

				}catch(Exception e){

				}

			}
			alglib.sparseconverttocrs(W);

			W.normalize (ratings.AllItems,k);
		}

		private IList<int> shuffleList(IList<int> list){
			list.Shuffle();
			return list;
		}

		/// <summary>
		/// Calculates sum(w_{l,l*}*l_{l*}^(1)
		/// </summary>
		/// <returns>The geographical weight sum of a location.</returns>
		/// <param name="l">Location.</param>
		private double[,] sumWeightWeatherFeature (int t1){
			double [,] sum = new double[K,1];
			for (int t2 = 0; t2 < rangeSize; t2++){
				for (int i = 0; i < K; i++) {
					sum [i, 0] +=	MFC [t1, t2] * F [t2,i];
				}
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

			try{
				alglib.sparsegetcompressedrow(W,l, ref colids, ref wrow, out tmp); // wrow --> k length  double[,] wll* = new double[K,k]   wll*[i] = wrow
			}catch(alglib.alglibexception e){
				Console.WriteLine (l);
				Console.WriteLine (e.Message);
			}
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

		private double computeRecommendationScoreFeature(int u, int l, DateTime time){
			return computeRecommendationScoreFeature (u, l, timeFeatureClassMapper [time]);
		}

		private double computeRecommendationScoreFeature(int u, int l, int featureclass){
			double[,] C = new double[1, 1];
			double[,] sum_weather_feature = this.sumWeatherFeatures [featureclass];
			C[0,0] = computeRecommendationScore (u, l);
			alglib.rmatrixgemm (1, 1, K, 1,F, featureclass, 0, 0, L2, idLocMapper [l], 0, 1, 0,ref C, 0, 0);
			alglib.rmatrixgemm (1, 1, K, 1,sum_weather_feature, 0, 0, 1, L3, idLocMapper [l], 0, 1, 1, ref C, 0, 0);
			return C[0,0];

		}

		/// <summary>
		/// Computes the recommendation score. equation 4 of [1]
		/// </summary>
		/// <returns>The recommendation score.</returns>
		/// <param name="u">U.</param>
		/// <param name="l">L.</param>
		private double computeRecommendationScore(int u, int l){
			double[,] sum_geo = this.sumGeos [l];
			double[,] C = new double[1, 1];
			try{
				alglib.rmatrixgemm (1, 1, K, 1,L1, idLocMapper [l], 0, 0, U1, idUserMapper [u], 0, 1, 0,ref C, 0, 0);
				alglib.rmatrixgemm (1, 1, K, 1,sum_geo, 0, 0, 1, U2, idUserMapper [u], 0, 1, 1, ref C, 0, 0);
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
			Console.WriteLine("epsilon = "+epsilon.ToString()+"; C = "+C.ToString()+"; gamma = "+gamma.ToString()+"; alpha = "+alpha.ToString()+"; k = "+k.ToString()+"; K = "+K.ToString()+"; WeatherAware = "+weather_aware.ToString()+"; beta = "+beta.ToString());
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

			Console.WriteLine ("Finished Init at:");
			Console.WriteLine (DateTime.Now);
		}

		public void adjustWeatherAwareBetweenLocations (int l1,int l2,int feature_class1 , int feature_class2 , int user, double eta){
			double gamma2 = 0.00001;
			double[,] sum_weather_featuret1 = this.sumWeatherFeatures [feature_class1];
			double[,] sum_weather_featuret2 = this.sumWeatherFeatures [feature_class2];
			IList<double> g = new List<double> ();
			for (int i = 0; i < K; i++) {
				g.Add(sum_weather_featuret2 [i,0] - sum_weather_featuret1 [i,0]);
			}
			IList<double> values1 = new List<double> ();
			IList<double> values2 = new List<double> ();
			IList<double> values3 = new List<double> ();
			IList<double> values4 = new List<double> ();
			for (int i = 0; i < K; i++) {
				values1.Add (F [feature_class1, i] - (gamma2 * eta * (L2 [idLocMapper [l2], i] - L2 [idLocMapper [l1], i])));
				values2 .Add(L3 [idLocMapper[l1], i] - (g[i]*gamma2*eta));
				values3 .Add(L2[idLocMapper[l2],i]-(F[feature_class1,i]*gamma2*eta));
				values4 .Add(L2[idLocMapper[l1],i]+(F[feature_class1,i]*gamma2*eta));

			}

			values1 = normalizeEuclidean (values1, beta * C);
			values2 = normalizeEuclidean (values2, beta * C);
			values3 = normalizeEuclidean (values3, beta * C);
			values4 = normalizeEuclidean (values4, beta * C);

			for (int i = 0; i < K; i++) {
				F [feature_class1, i] = values1 [i];
				L3 [idLocMapper[l1], i] = values2 [i];
				L2 [idLocMapper [l2],i] = values3 [i];
				L2 [idLocMapper [l1],i] = values4 [i];
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
			double[,] suml1 = this.sumGeos [l1];
			double[,] suml2 = this.sumGeos [l2];
			IList<double> g = new List<double> ();
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

		public int mapFeatureInRange(double feature){
			return Convert.ToInt32((feature - minFeature) / (maxFeature - minFeature) * (rangeSize-1));
		}

		public void createTimeFeatureMapper(){
			DBConnect conn = new DBConnect (connection_string);
			List<string>[] res;
			if (cityId != 0) {
				res = conn.Select ("select max(" + weatherFeature + "), min(" + weatherFeature + ") from DAILY_WEATHER" +
				                     " dw inner join HOURLY_WEATHER hw on(hw.daily_weather_id = dw.id) where city_id = " + cityId.ToString (), 2);
				maxFeature = double.Parse (res [0] [0]);
				minFeature = double.Parse (res [1] [0]);
				res = conn.Select ("select hw.unix_utc_timestamp, " + weatherFeature + " from DAILY_WEATHER" +
				" dw inner join HOURLY_WEATHER hw on(hw.daily_weather_id = dw.id) where city_id = " + cityId.ToString () + " and " + weatherFeature + " is not NULL ", 2);

			} else {
				res = conn.Select ("select max(" + weatherFeature + "), min(" + weatherFeature + ") from DAILY_WEATHER" +
					" dw inner join HOURLY_WEATHER hw on(hw.daily_weather_id = dw.id) ", 2);
				maxFeature = double.Parse (res [0] [0]);
				minFeature = double.Parse (res [1] [0]);
				res = conn.Select ("select hw.unix_utc_timestamp, " + weatherFeature + " from DAILY_WEATHER" +
					" dw inner join HOURLY_WEATHER hw on(hw.daily_weather_id = dw.id) where " + weatherFeature +" is not NULL " , 2);
			}
			List<string> timestamps = res [0];
			List<string> feature = res [1];
			timeFeatureClassMapper = new Dictionary<DateTime, int> ();

			int i = 0;
			foreach (string timestamp in timestamps) {
				var time = new DateTime (int.Parse (timestamp) * 10000000L).AddYears (1969);
				var offset = TimeZone.CurrentTimeZone.GetUtcOffset (time);
				if(!timeFeatureClassMapper.ContainsKey(time-offset))
					timeFeatureClassMapper.Add (time - offset, mapFeatureInRange (double.Parse (feature [i])));
				i++;
			}
		}

		/// <summary>
		/// Returns 
		/// </summary>
		/// <returns>The feature of times.</returns>
		/// <param name="user_items_time">User items time.</param>
		public Dictionary<int,IList<Tuple<int,int>>> getFeatureOfTimes(Dictionary<int,IList<Tuple<int,DateTime>>> user_items_time){
			Dictionary<int,IList<Tuple<int,int>>> user_items_feature = new Dictionary<int,IList<Tuple<int,int>>> ();
			DBConnect conn = new DBConnect (connection_string);

			foreach (int user in user_items_time.Keys.ToList()) {
				IList<Tuple<int,DateTime>> item_time = user_items_time [user];
				IList<Tuple<int,int>> item_feature = new List<Tuple<int,int>> ();
				foreach (Tuple<int,DateTime> item_time_tuple in item_time) {
					item_feature.Add (Tuple.Create(item_time_tuple.Item1, timeFeatureClassMapper [item_time_tuple.Item2]));
				}
				user_items_feature.Add (user, item_feature);
			}

			return user_items_feature;

		}
			
		private void evaluate(int i){
			Console.WriteLine ("Start evaluation at iteration "+i.ToString());
			double result = Eval.ItemsWeatherItemRecommender.EvaluateTime (this, this.validation_ratings, this.timed_ratings,"VALIDATION ", weather_aware);
			result = Eval.ItemsWeatherItemRecommender.EvaluateTime (this, this.test_ratings, this.timed_ratings,"TEST " , weather_aware);

			Console.Write ("Finished evaluation after: ");
			Console.WriteLine (DateTime.Now);
		}
			
		private Dictionary<DateTime, int> timeFeatureClassMapper;

		private double[,] MFC;

		private void normalizeMFCMatrix(){
			for (int t1 = 0; t1 < rangeSize; t1++) {
				double sum = 0;
				for (int t2 = 0; t2 < rangeSize; t2++) {
					sum += MFC [t1, t2];
				}
				double sum2 = 0;
				for (int t2 = 0; t2 < rangeSize; t2++) {
					if (sum != 0)
						MFC [t1, t2] = MFC [t1, t2] / sum;
					else
						MFC [t1, t2] = 0;
					sum2 += MFC [t1, t2];
				}
			}
		}

		private void initMFCMatrix(int[,,] UserItemFeatureclassTensor){
			MFC = new double[rangeSize, rangeSize];
			for (int t1 = 0; t1 < rangeSize; t1++) {
				for (int t2 = 0; t2 < rangeSize; t2++) {
					if (t1 != t2) {
						double numeratorsum = 0;
						double denominatorsum1 = 0;
						double denominatorsum2 = 0;
						foreach (int user in idUserMapper.Values) {
							foreach (int location in idLocMapper.Values) {
								numeratorsum += UserItemFeatureclassTensor [user, location, t1] * UserItemFeatureclassTensor [user, location, t2];
								denominatorsum1 += Math.Pow (UserItemFeatureclassTensor [user, location, t1], 2);
								denominatorsum2 += Math.Pow (UserItemFeatureclassTensor [user, location, t2], 2);
							}
						}
						double denominator = (Math.Sqrt (denominatorsum1) * Math.Sqrt (denominatorsum2));
						if (denominator == 0)
							MFC[t1,t2] = 0;
						else
							MFC [t1, t2] = numeratorsum / denominator;
					} else {
						MFC [t1, t2] = 0;
					}
				}
			}
			normalizeMFCMatrix ();
		}

		/// <summary>
		/// Learn the model parameters of the recommender from the training data
		/// </summary>
		/// <remarks></remarks>
		public override void Train()
		{
			init ();
			createTimeFeatureMapper ();

			Dictionary<int,IList<Tuple<int,DateTime>>> user_items_time = timed_ratings.getItemsUserDictWithTime ();

			Dictionary<int,IList<Tuple<int,int>>> user_items_featureclass = getFeatureOfTimes (user_items_time);

			Dictionary<int,IList<DateTime>> itemsTime = timed_ratings.getTimesItemDict ();

			int[, ,] UserItemFeatureclassTensor = new int[idUserMapper.Keys.Count, idLocMapper.Keys.Count , this.rangeSize];  // user, item, featureclass

			foreach (KeyValuePair<int,IList<Tuple<int,int>>> entry in user_items_featureclass) {
				foreach (Tuple<int,int> item_featureclass in entry.Value) {
					try{
						UserItemFeatureclassTensor [idUserMapper [entry.Key], idLocMapper [item_featureclass.Item1], item_featureclass.Item2] += 1;
					}catch(Exception e){
						Console.WriteLine ("<<<<<<<<<<<<<<<<<<<<");
						Console.WriteLine (idUserMapper [entry.Key]);
						Console.WriteLine (idLocMapper [item_featureclass.Item1]);
						Console.WriteLine (item_featureclass.Item2);
						Console.WriteLine (idUserMapper.Keys.Count);
						Console.WriteLine (idLocMapper.Keys.Count );
						Console.WriteLine (this.rangeSize);
						Console.WriteLine (">>>>>>>>>>>>>>>>>>>>");
					}
				}
			}

			initMFCMatrix (UserItemFeatureclassTensor);
				
			System.Random gen = new System.Random();
			this.timed_ratings.AllUsers.Shuffle ();
			int i = 0;
			while (i <= max_iter) {
				Console.Write ("Start iteration: " + i.ToString () + " at: ");
				Console.WriteLine (DateTime.Now);
				ParallelOptions po = new ParallelOptions{
					MaxDegreeOfParallelism = Environment.ProcessorCount
				};
				IList<int> locations = shuffleList (this.timed_ratings.AllItems);

				sumGeos = new Dictionary<int,double[,]> ();
				foreach (int l in idLocMapper.Keys.ToList()) {
					sumGeos.Add(l, sumWeightOfTwoLocationsGeo (l));
				}

				sumWeatherFeatures = new Dictionary<int,double[,]> ();
				for (int t = 0; t < rangeSize; t++) {
					sumWeatherFeatures.Add(t, sumWeightWeatherFeature(t));
				}

				Parallel.ForEach (this.timed_ratings.AllUsers, po, user => {
					foreach (Tuple<int,int> item_featureclass in user_items_featureclass[user].Distinct()) {
						double y_ul = 0, y_ul2 = 0;
						int x_ul = 0, x_ul2 = 0, n = 0, finall2 = 0;
						foreach(DateTime time in itemsTime[item_featureclass.Item1]){
							x_ul += UserItemFeatureclassTensor [idUserMapper [user], idLocMapper [item_featureclass.Item1], timeFeatureClassMapper [time]];
						}
						if (x_ul == 0)
							throw new Exception ();
						y_ul = computeRecommendationScore(user, item_featureclass.Item1);


						foreach (int l2 in locations) {
							if (l2 == item_featureclass.Item1)
								continue;
							y_ul2 = computeRecommendationScore(user, l2);
							foreach(DateTime time in itemsTime[l2]){
								x_ul2 += UserItemFeatureclassTensor [idUserMapper [user], idLocMapper [l2], timeFeatureClassMapper [time]];
							}
							n++;
							if (incompFunction (x_ul, x_ul2, y_ul, y_ul2) == 1) {
								finall2 = l2;
								break;
							}
						}
						if (incompFunction (x_ul, x_ul2, y_ul, y_ul2) == 1) {
							double eta = E ((int)(this.ratings.AllItems.Count / n)) * deltaFunction (y_ul, y_ul2);
							adjustGeo (item_featureclass.Item1, finall2, user, eta);
						}

						if (weather_aware){
							double y_ulf = 0, y_ulf2 = 0;
							int x_ulf = 0, x_ulf2 = 0;
							n = 0;
							finall2 = 0;
							x_ulf = UserItemFeatureclassTensor [idUserMapper [user], idLocMapper [item_featureclass.Item1], item_featureclass.Item2];
							if (x_ulf == 0)
								throw new Exception ();
							y_ulf = computeRecommendationScoreFeature(user, item_featureclass.Item1, item_featureclass.Item2);
							DateTime finalTime = DateTime.Now;
							foreach (int l2 in locations) {
								if (l2 == item_featureclass.Item1)
									continue;
								bool breaking= false;
								foreach(DateTime time in itemsTime[l2]){
									y_ulf2 = computeRecommendationScoreFeature(user, l2, timeFeatureClassMapper [time]); //computeRecommendationScoreFeature (user, l2, timeFeatureClassMapper[time]);
									x_ulf2 = UserItemFeatureclassTensor [idUserMapper [user], idLocMapper [l2], timeFeatureClassMapper [time]]; 
									n++;
									if (incompFunction (x_ulf, x_ulf2, y_ulf, y_ulf2) == 1) {
										finall2 = l2;
										finalTime = time;
										breaking = true;
										break;
									}
								}
								if (breaking)
									break;
							}
							if (incompFunction (x_ulf, x_ulf2, y_ulf, y_ulf2) == 1) {
								double eta = E ((int)(this.ratings.AllItems.Count / n)) * deltaFunction (y_ulf, y_ulf2);
								adjustWeatherAwareBetweenLocations (item_featureclass.Item1, finall2, item_featureclass.Item2, timeFeatureClassMapper [finalTime], user, eta);
							}
						}
					}
				});
				Console.Write ("Finished iteration " + i.ToString ()+ " at: ");
				Console.WriteLine (DateTime.Now);
				if (i % evaluation_at == 0 && i != 0) {
					evaluate (i);
				}
				i++;

			}

			sumGeos = new Dictionary<int,double[,]> ();
			foreach (int l in idLocMapper.Keys.ToList()) {
				sumGeos.Add(l, sumWeightOfTwoLocationsGeo (l));
			}

			sumWeatherFeatures = new Dictionary<int,double[,]> ();
			for (int t = 0; t < rangeSize; t++) {
				sumWeatherFeatures.Add(t, sumWeightWeatherFeature(t));
			}

		}
	}
}


