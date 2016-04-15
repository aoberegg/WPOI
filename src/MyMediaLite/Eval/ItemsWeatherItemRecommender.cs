// Copyright (C) 2015 Zeno Gantner
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
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using MyMediaLite.Data;
using MyMediaLite.DataType;
using MyMediaLite.Eval.Measures;
using MyMediaLite.Eval;
using MyMediaLite.ItemRecommendation;
using MyMediaLite.RatingPrediction;

namespace MyMediaLite.Eval
{
	public static class ItemsWeatherItemRecommender
	{
		/// <summary>the evaluation measures for item prediction offered by the class</summary>
		/// <remarks>
		/// The evaluation measures currently are:
		/// <list type="bullet">
		///   <item><term>AUC</term><description>area under the ROC curve</description></item>
		///   <item><term>prec@5</term><description>precision at 5</description></item>
		///   <item><term>prec@10</term><description>precision at 10</description></item>
		///   <item><term>MAP</term><description>mean average precision</description></item>
		///   <item><term>recall@5</term><description>recall at 5</description></item>
		///   <item><term>recall@10</term><description>recall at 10</description></item>
		///   <item><term>NDCG</term><description>normalizad discounted cumulative gain</description></item>
		///   <item><term>MRR</term><description>mean reciprocal rank</description></item>
		/// </list>
		/// An item recommender is better than another according to one of those measures its score is higher.
		/// </remarks>
		static public ICollection<string> Measures
		{
			get {
				string[] measures = { "AUC", "prec@5", "prec@10", "MAP", "recall@5", "recall@10", "NDCG", "MRR" };
				return new HashSet<string>(measures);
			}
		}
			
//		/// <summary>
//		/// Gets string for subselecting all id's used from database
//		/// </summary>
//		/// <returns>The all identifiers string for database.</returns>
//		static private string getAllIdsStringForDatabase(IList<int> allItems){
//
//			string all_ids = "(";
//			bool first = true;
//			foreach (int id in allItems) {
//				if (first) {
//					all_ids += id.ToString ();
//					first = false;
//				} else
//					all_ids += "," + id.ToString ();
//			}
//			all_ids += ")";
//			return all_ids;
//		}
//
////		static public void getWeatherVectorLocation(IList<int> items, string connection_string, ref Dictionary<int,IList<double>> venueWeatherVectors){
////			DBConnect conn = new DBConnect (connection_string);
////			List<string>[] res;
////			res = conn.Select ("select * " +
////			" from weather_avgs_per_venue where id_int in "+getAllIdsStringForDatabase(items), 9);
////			List<string> all_ids = res [0];
////			List<string> temperature = res [1];
////			List<string> precip_intensity = res [2];
////			List<string> wind_speed = res [3];
////			List<string> humidity = res [4];
////			List<string> cloud_cover = res [5];
////			List<string> pressure = res [6];
////			List<string> visibility = res [7];
////			List<string> moonphase = res [8];
////			int i = 0;
////			foreach(string id in all_ids){
////				venueWeatherVectors.Add(int.Parse (id),new List<double> { double.Parse(temperature [i]), double.Parse(precip_intensity [i]), double.Parse(wind_speed [i]), double.Parse(humidity [i]),
////					double.Parse(cloud_cover [i])});
////				i++;
////			}
////		}


		/// <summary>Evaluation for rankings of items</summary>
		/// <remarks>
		/// User-item combinations that appear in both sets are ignored for the test set, and thus in the evaluation,
		/// except the boolean argument repeated_events is set.
		///
		/// The evaluation measures are listed in the Measures property.
		/// Additionally, 'num_users' and 'num_items' report the number of users that were used to compute the results
		/// and the number of items that were taken into account.
		///
		/// Literature:
		/// <list type="bullet">
		///   <item><description>
		///   C. Manning, P. Raghavan, H. Schütze: Introduction to Information Retrieval, Cambridge University Press, 2008
		///   </description></item>
		/// </list>
		///
		/// On multi-core/multi-processor systems, the routine tries to use as many cores as possible,
		/// which should to an almost linear speed-up.
		/// </remarks>
		/// <param name="recommender">item recommender</param>
		/// <param name="test">test cases</param>
		/// <param name="training">training data</param>
		/// <param name="n">length of the item list to evaluate -- if set to -1 (default), use the complete list, otherwise compute evaluation measures on the top n items</param>
		/// <returns>a dictionary containing the evaluation results (default is false)</returns>
//		static public ItemRecommendationEvaluationResults Evaluate(
//			this IRecommender recommender,
//			ITimedRatings test,
//			ITimedRatings training,
//			string connection_string = "",
//			int n = -1,double alpha = 0.1)
//		{
//
//			var result = new ItemRecommendationEvaluationResults();
//			var candidates = test.AllItems.Intersect(training.AllItems).ToList();
//			int num_users = 0;
//			ThreadPool.SetMinThreads(test.AllUsers.Count, test.AllUsers.Count);
//			Dictionary<int,IList<int>> user_items = test.getItemsUserDict ();
//			ParallelOptions po = new ParallelOptions{
//				MaxDegreeOfParallelism = Environment.ProcessorCount
//			};
//					
//			//foreach(int user_id in test.AllUsers){
//			Parallel.ForEach (test.AllUsers, po, user_id => {
//				try {
//					n = user_items [user_id].Count;
//					IList<Tuple<int,float>> prediction;
//					prediction = recommender.Recommend (user_id, candidate_items: candidates, n: n);
//					var prediction_list = (from t in prediction select t.Item1).ToArray ();
//					int num_candidates_for_this_user = candidates.Count ();
//					int num_dropped_items = num_candidates_for_this_user - prediction.Count;
//					var correct_items = user_items [user_id].Intersect (candidates).ToList ();
//					if (correct_items.Count () == 0)
//						return;
//
//					double auc = AUC.Compute (prediction_list, correct_items, num_dropped_items);
//					double map = PrecisionAndRecall.AP (prediction_list, correct_items);
//					double ndcg = NDCG.Compute (prediction_list, correct_items);
//					double rr = ReciprocalRank.Compute (prediction_list, correct_items);
//					var positions = new int[] { 5, 10 };
//					var prec = PrecisionAndRecall.PrecisionAt (prediction_list, correct_items, positions);
//					var recall = PrecisionAndRecall.RecallAt (prediction_list, correct_items, positions);
//
//					// thread-safe incrementing
//					lock (result) {
//						num_users++;
//						result ["AUC"] += (float)auc;
//						result ["MAP"] += (float)map;
//						result ["NDCG"] += (float)ndcg;
//						result ["MRR"] += (float)rr;
//						result ["prec@5"] += (float)prec [5];
//						result ["prec@10"] += (float)prec [10];
//						result ["recall@5"] += (float)recall [5];
//						result ["recall@10"] += (float)recall [10];
//					}
//
//					if (num_users % 1000 == 0)
//						Console.Error.Write (".");
//					if (num_users % 60000 == 0)
//						Console.Error.WriteLine ();
//				} catch (Exception e) {
//					Console.Error.WriteLine ("===> ERROR: " + e.Message + e.StackTrace);
//					throw;
//				}
//			});
//
//			foreach (string measure in Measures)
//				result[measure] /= num_users;
//			result["num_users"] = num_users;
//			result["num_lists"] = num_users;
//			result["num_items"] = candidates.Count();
//
//			return result;
//		}
			

		static public double EvaluateTime(
			this IRecommender recommender,
			ITimedRatings test,
			ITimedRatings training,
			string dataset ,
			bool time_aware,
			int n = -1,double alpha = 0.1)
		{

			Dictionary<int,ItemRecommendationEvaluationResults> userRecommendationResults = new Dictionary<int,ItemRecommendationEvaluationResults> ();
			foreach (int user in test.AllUsers)
				userRecommendationResults.Add (user, new ItemRecommendationEvaluationResults ());
			
			var candidates = test.AllItems.Intersect(training.AllItems).ToList();
			ParallelOptions po = new ParallelOptions{
				MaxDegreeOfParallelism = Environment.ProcessorCount
			};
			bool init = true;
			Dictionary<int, IList<int>> trainingUserItems = training.getItemsUserDict ();
			Parallel.For (0, test.Users.Count - 1, po, index => {
				try{	

					DateTime time = test.Times[index];

					int user = test.Users[index];
					int item = test.Items[index];
					if (trainingUserItems[user].Contains(item))
						return;
					IList<int> correct_items = new List<int>();
					correct_items.Add(item);
					correct_items = correct_items.Intersect (candidates).ToList ();
					if (correct_items.Count () == 0)
						return;
					IList<Tuple<int,float>> prediction;
					if (time_aware)
						prediction = ((ITimeAwareRatingPredictor)recommender).RecommendTime(user,time ,candidate_items:candidates,n:20);
					else
						prediction = recommender.Recommend(user, candidate_items: candidates, n:20);
					var prediction_list = (from t in prediction select t.Item1).ToArray ();

					double auc = AUC.Compute (prediction_list, correct_items, 0);
					double map = PrecisionAndRecall.AP (prediction_list, correct_items);
					double ndcg = NDCG.Compute (prediction_list, correct_items);
					double rr = ReciprocalRank.Compute (prediction_list, correct_items);
					var positions = new int[] { 5, 10 };
					var prec = PrecisionAndRecall.PrecisionAt (prediction_list, correct_items, positions);
					var recall = PrecisionAndRecall.RecallAt (prediction_list, correct_items, positions);

					lock(userRecommendationResults){
						ItemRecommendationEvaluationResults res = userRecommendationResults[user];
						res["AUC"] += (float)auc;
						res["MAP"] += (float)map;
						res["NDCG"] += (float)ndcg;
						res["MRR"] += (float)rr;
						res["prec@5"] += (float)prec [5];
						res["prec@10"] += (float)prec [10];
						res["recall@5"] += (float)recall [5];
						res["recall@10"] += (float)recall [10];
						if(!init){
							res["AUC"] /= 2;
							res["MAP"] /= 2;
							res["NDCG"] /= 2;
							res["MRR"] /= 2;
							res["prec@5"] /= 2;
							res["prec@10"] /= 2;
							res["recall@5"] /= 2;
							res["recall@10"] /= 2;
						}
						init = false;
						userRecommendationResults[user] = res;
					}
				} catch (Exception e) {
					Console.Error.WriteLine ("===> ERROR: " + e.Message + e.StackTrace);
					throw;
				}
			});
			ItemRecommendationEvaluationResults avg_res = new ItemRecommendationEvaluationResults ();
			int num_users = 0;
			Console.WriteLine ("Detailed user results:");
			foreach (int user in userRecommendationResults.Keys) {
				Console.Write ("User: ");
				Console.WriteLine (user);
				foreach (var key in userRecommendationResults[user].Keys) {
					avg_res [key] += userRecommendationResults [user] [key];
					Console.WriteLine ("{0}={1}", key, userRecommendationResults [user] [key]);
				}
				num_users++;
			}
			foreach (string measure in Measures)
				avg_res[measure] /= num_users;
			Console.WriteLine (dataset + " Avg results:");
			foreach (var key in avg_res.Keys)
				Console.WriteLine ("{0}={1}", key, avg_res[key]);
			return avg_res["prec@5"];
		}
	}
}
