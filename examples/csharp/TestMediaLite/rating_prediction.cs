using System;
using System.Collections.Generic;
using System.Linq;
using MyMediaLite.Data;
using MyMediaLite.Eval;
using MyMediaLite.IO;
using MyMediaLite.RatingPrediction;
using MyMediaLite.DataType;

using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Extreme.Mathematics.LinearAlgebra;

public class RatingPrediction
{
	public static string connection;

	private static void create_data(int user_id, List<int> item_list, List<DateTime> time_list, ref ITimedRatings data){
		for (int i = 0; i < item_list.Count; i++) {
			data.Add (user_id, item_list [i], 0, time_list [i]);
		}
	}

	private static void create_data(List<int> user_list, List<int> item_list, List<DateTime> time_list, ref ITimedRatings data){

		for (int i = 0; i < item_list.Count; i++) {
			data.Add (user_list[i], item_list [i], 0, time_list [i]);
		}
	}

	private static ITimedRatings readData(string data_file){
		Console.WriteLine("Dataset: "+ data_file);
		Console.WriteLine (DateTime.Now);
		var all_data = TimedRatingData.Read(data_file,null, null, TestRatingFileFormat.WITHOUT_RATINGS, true);
		Console.Write(all_data.Statistics ());
		Console.WriteLine ("Finished dataset read");
		return all_data;

	}

	private static ITimedRatings readDataMapped(string data_file, ref MyMediaLite.Data.Mapping user_mappings, ref MyMediaLite.Data.Mapping item_mappings){
		Console.WriteLine("Dataset: "+ data_file);

		Console.WriteLine (DateTime.Now);
		var all_data = TimedRatingData.Read(data_file,user_mappings, item_mappings, TestRatingFileFormat.WITHOUT_RATINGS, true);
		Console.Write(all_data.Statistics ());
		Console.WriteLine ("Finished dataset read");
		return all_data;

	}

	private static void getSubset (ITimedRatings all_data, ref ITimedRatings sub_all_data){
		List<int> sub_list_item = new List<int>();
		List<DateTime> sub_list_time = new List<DateTime>();
		List<int> sub_list_user = new List<int>();
		System.Random gen = new System.Random();

		IList<int> all_users = all_data.AllUsers;
		IList<int> users = new List<int> ();
		for (int i = 0; i < all_users.Count; i++) {
			int rnd = gen.Next(100);
			if (rnd <= 2) {
				users.Add(all_users[i]);
			}
		}

		for (int i = 0; i < all_data.Users.Count; i++) {
			if (users.Contains (all_data.Users [i])) {
				sub_list_item.Add (all_data.Items [i]);
				sub_list_time.Add (all_data.Times [i]);
				sub_list_user.Add (all_data.Users [i]);
			}
		}
		create_data(sub_list_user,  sub_list_item, sub_list_time, ref sub_all_data);
		Console.Write (sub_all_data.Statistics ());
		Console.WriteLine ("finished creating subset:");
		Console.WriteLine (DateTime.Now);
	}
		

//	private static void readAndSplitDataRandomly(ITimedRatings all_data, ref ITimedRatings validation_data, ref ITimedRatings test_data, ref ITimedRatings training_data){
//
//
//		Console.WriteLine (all_data.Statistics ());
//
//		List<int> validation_list_item = new List<int>();
//		List<int> test_list_item = new List<int>();
//		List<int> training_list_item = new List<int>();
//
//		List<DateTime> validation_list_time = new List<DateTime>();
//		List<DateTime> test_list_time = new List<DateTime>();
//		List<DateTime> training_list_time = new List<DateTime>();
//
//		List<int> validation_list_user = new List<int>();
//		List<int> test_list_user = new List<int>();
//		List<int> training_list_user = new List<int>();
//
//		System.Random gen = new System.Random();
//		for (int i = 0; i < all_data.Users.Count; i++) {
//			int rnd = gen.Next(100);
//			if(rnd <= 10){
//				validation_list_item.Add(all_data.Items[i]);
//				validation_list_time.Add(all_data.Times[i]);
//				validation_list_user.Add(all_data.Users[i]);
//			}else if(rnd <=30){
//				test_list_item.Add(all_data.Items[i]);
//				test_list_time.Add(all_data.Times[i]);
//				test_list_user.Add(all_data.Users[i]);
//			}else{
//				training_list_item.Add(all_data.Items[i]);
//				training_list_time.Add(all_data.Times[i]);
//				training_list_user.Add(all_data.Users[i]);
//			}
//		}
//		create_data(validation_list_user,  validation_list_item, validation_list_time, ref validation_data);
//		create_data(test_list_user,  test_list_item, test_list_time, ref test_data);
//		create_data(training_list_user,  training_list_item, training_list_time, ref training_data);
//
//
//
//		Console.Write(validation_data.Statistics ());
//		Console.Write(test_data.Statistics ());
//		Console.Write(training_data.Statistics());
//		Console.WriteLine ("finished creating datasets:");
//		Console.WriteLine (DateTime.Now);
//	}
//
	private static void readAndSplitData(ITimedRatings all_data, ref ITimedRatings test_data, ref ITimedRatings training_data, ref ITimedRatings validation_data){
		Dictionary<int,IList<DateTime>> user_times = all_data.getTimesUserDict();
		Dictionary<int,IList<int>> user_items = all_data.getItemsUserDict ();

		foreach (int user_id in all_data.AllUsers) {
			List<DateTime> timesOfUser = (List<DateTime>)user_times [user_id];
			List<int> itemsOfUser = (List<int>) user_items[user_id];
			int amountCheckIns = timesOfUser.Count;
			//int validation = (int)(amountCheckIns * 0.1);
			int test = (int)(amountCheckIns * 0.2);
			int training = (int)(amountCheckIns * 0.7);

			List<int> training_list_item = itemsOfUser.GetRange(0, training);
			List<int> test_list_item = itemsOfUser.GetRange (training, amountCheckIns-(training));
			List<int> validation_list_item = itemsOfUser.GetRange (training + test,amountCheckIns-(training+test));

			List<DateTime> training_list_time = timesOfUser.GetRange(0, training);
			List<DateTime> test_list_time = timesOfUser.GetRange (training, amountCheckIns-(training));
			List<DateTime> validation_list_time = timesOfUser.GetRange (training + test,amountCheckIns-(training+test));

			create_data(user_id,  validation_list_item, validation_list_time, ref validation_data);
			create_data(user_id,  test_list_item, test_list_time, ref test_data);
			create_data(user_id,  training_list_item, training_list_time, ref training_data);
		}

		Console.Write(validation_data.Statistics ());
		Console.Write(test_data.Statistics ());
		Console.Write(training_data.Statistics());
		Console.WriteLine ("finished creating datasets:");
		Console.WriteLine (DateTime.Now);
	}
//
	private static void readAndSplitDataRandomly(ITimedRatings all_data, ref ITimedRatings test_data, ref ITimedRatings training_data){

		Dictionary<int,IList<DateTime>> user_times = all_data.getTimesUserDict();
		Dictionary<int,IList<int>> user_items = all_data.getItemsUserDict ();

		List<int> test_list_item = new List<int>();
		List<int> training_list_item = new List<int>();


		List<DateTime> test_list_time = new List<DateTime>();
		List<DateTime> training_list_time = new List<DateTime>();

		List<int> test_list_user = new List<int>();
		List<int> training_list_user = new List<int>();

		System.Random gen = new System.Random();
		for (int i = 0; i < all_data.Users.Count; i++) {
			int rnd = gen.Next(100);
			if(rnd <=30){
				test_list_item.Add(all_data.Items[i]);
				test_list_time.Add(all_data.Times[i]);
				test_list_user.Add(all_data.Users[i]);
			}else{
				training_list_item.Add(all_data.Items[i]);
				training_list_time.Add(all_data.Times[i]);
				training_list_user.Add(all_data.Users[i]);
			}
		}
		create_data(test_list_user,  test_list_item, test_list_time, ref test_data);
		create_data(training_list_user,  training_list_item, training_list_time, ref training_data);

		Console.Write(test_data.Statistics ());
		Console.Write(training_data.Statistics());
		Console.WriteLine ("finished creating datasets:");
		Console.WriteLine (DateTime.Now);
	}

	private static void writeAvgResults(List<MyMediaLite.Eval.ItemRecommendationEvaluationResults> result_list){
		MyMediaLite.Eval.ItemRecommendationEvaluationResults avg_results = new MyMediaLite.Eval.ItemRecommendationEvaluationResults ();
		int j = 0;
		foreach (MyMediaLite.Eval.ItemRecommendationEvaluationResults result in result_list) {
			foreach(var key in result.Keys){
				if (!avg_results.ContainsKey (key))
					avg_results.Add(key, 0f);
				avg_results[key] += result[key];
			}
			j+=1;
		}
		Console.WriteLine("Avg results after "+j.ToString()+" iterations.");
		foreach(var key in avg_results.Keys){
			Console.WriteLine("{0}={1}", key, avg_results [key]/j);
		}
	}

	/// <summary>
	/// Initialize a matrix with normal distributed values with mean = 0.0 std = 0.01
	/// </summary>
	/// <param name="ids">Identifiers.</param>
	/// <param name="M">Matrix to initialize</param>
	private static void initMatrixNormal(int size, ref double[,] M, int K){
		MathNet.Numerics.Distributions.Normal normalDist = new MathNet.Numerics.Distributions.Normal(0.0, 0.01);
		for(int i = 0; i < size; i++){
			for (int j = 0; j< K; j++){
				M [i, j] = normalDist.Sample ();
			}
		}
	}

	/// <summary>
	/// Initialize a matrix with normal distributed values with mean = 0.0 std = 0.01
	/// </summary>
	/// <param name="ids">Identifiers.</param>
	/// <param name="M">Matrix to initialize</param>
	private static void initMatrixNormal(IList<int> ids, ref double[,] M, ref Dictionary<int,int> mapper ,int K){
		MathNet.Numerics.Distributions.Normal normalDist = new MathNet.Numerics.Distributions.Normal(0.0, 0.01);
		int i = 0;
		foreach(int id in ids){
			for (int j = 0; j< K; j++){
				M [i, j] = normalDist.Sample ();
				mapper [id] = i;
			}
			i++;
		}
	}



	private static void startIterationTest(ITimedRatings all_data, bool weather_aware, double beta, double mu, int city, int iterations){
		removeUserThreshold(ref all_data);
		Console.WriteLine ("Start iteration test");
		ITimedRatings validation_data = new TimedRatings (); // 10%
		ITimedRatings test_data = new TimedRatings (); // 30%
		ITimedRatings training_data = new TimedRatings (); // 70%
		readAndSplitData (all_data, ref test_data, ref training_data, ref validation_data);

		int K = 100;
		int rangeSize = 10;
		int user_count = all_data.AllUsers.Count;
		int location_count = all_data.AllItems.Count ();

		double[,] U1 = new double[user_count, K];
		double[,] U2 = new double[user_count, K];
		double[,] U3 = new double[user_count, K];
		double[,] L1 = new double[location_count, K];

		Dictionary<int,int> idMapperLocations = new Dictionary<int,int> ();
		Dictionary<int,int> idMapperUser = new Dictionary<int,int> ();

		initMatrixNormal (all_data.AllUsers, ref U1, ref idMapperUser, K);
		initMatrixNormal (all_data.AllUsers, ref U2, ref idMapperUser, K);
		initMatrixNormal (all_data.AllUsers, ref U3, ref idMapperUser, K);
		initMatrixNormal (all_data.AllItems, ref L1, ref idMapperLocations, K);

		WeatherItemRecommender recommender = new WeatherItemRecommender (U1, U2, U3, L1, idMapperLocations, idMapperUser);
		recommender.connection_string = connection;
		recommender.Ratings = training_data;
		recommender.Validation = validation_data;
		recommender.Test = test_data;
		recommender.weather_aware = weather_aware;
		recommender.max_iter = iterations;
		recommender.evaluation_at = 20;
		recommender.beta = beta;
		DateTime start_time = DateTime.Now;
		recommender.Train ();
		Console.Write ("Total Training time needed:");
		Console.WriteLine (((TimeSpan)(DateTime.Now - start_time)).TotalMilliseconds);
		Console.WriteLine ("Final results in this iteration:");
		var results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, validation_data, training_data, "VALIDATION ", false);
		results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, test_data, training_data,"TEST ",false);
	}


	private static void startIterationTestNew(ITimedRatings all_data, bool weather_aware, double beta, double mu, int city, int iterations, string feature = ""){
		removeUserThreshold(ref all_data);
		Console.WriteLine ("Start iteration test");
		ITimedRatings validation_data = new TimedRatings (); // 10%
		ITimedRatings test_data = new TimedRatings (); // 30%
		ITimedRatings training_data = new TimedRatings (); // 70%
		readAndSplitData (all_data, ref test_data, ref training_data, ref validation_data);

		int K = 100;
		int rangeSize = 10;
		int user_count = all_data.AllUsers.Count;
		int location_count = all_data.AllItems.Count ();

		double[,] U1 = new double[user_count, K];
		double[,] U2 = new double[user_count, K];
		double[,] L1 = new double[location_count, K];
		double[,] L2 = new double[location_count, K];
		double[,] L3 = new double[location_count, K];
		double[,] F = new double[rangeSize, K];
		Dictionary<int,int> idMapperCategories = new Dictionary<int,int> ();
		Dictionary<int,int> idMapperLocations = new Dictionary<int,int> ();
		Dictionary<int,int> idMapperUser = new Dictionary<int,int> ();

		initMatrixNormal (all_data.AllUsers, ref U1, ref idMapperUser, K);
		initMatrixNormal (all_data.AllUsers, ref U2, ref idMapperUser, K);
		initMatrixNormal (all_data.AllItems, ref L1, ref idMapperLocations, K);
		initMatrixNormal (all_data.AllItems, ref L2, ref idMapperLocations, K);
		initMatrixNormal (all_data.AllItems, ref L3, ref idMapperLocations, K);
		initMatrixNormal (rangeSize, ref F, K);
		WeatherContextAwareItemRecommender recommender = new WeatherContextAwareItemRecommender (U1, U2, L1, L2, L3, F, idMapperLocations, idMapperUser, city, feature);
		recommender.connection_string = connection;
		recommender.Ratings = training_data;
		recommender.Validation = validation_data;
		recommender.Test = test_data;
		recommender.weather_aware = weather_aware;
		recommender.rangeSize = rangeSize;
		recommender.max_iter = iterations;
		recommender.evaluation_at = 20;
		recommender.beta = beta;
		DateTime start_time = DateTime.Now;
		recommender.Train ();
		Console.Write ("Total Training time needed:");
		Console.WriteLine (((TimeSpan)(DateTime.Now - start_time)).TotalMilliseconds);
		Console.WriteLine ("Final results in this iteration:");

		//}
	}


	private static void startMostPopular(ITimedRatings all_data){

		removeUserThreshold (ref all_data);

		Console.WriteLine("Start iteration Test Most Popular ");
		//for (int i = 0; i < 5; i++) {
		ITimedRatings validation_data = new TimedRatings (); // 10%
		ITimedRatings test_data = new TimedRatings (); // 20%
		ITimedRatings training_data = new TimedRatings (); // 70%
		readAndSplitData (all_data, ref test_data, ref training_data, ref validation_data);
		IPosOnlyFeedback training_data_pos = new PosOnlyFeedback<SparseBooleanMatrix> (); // 80%
		for (int index = 0; index < training_data.Users.Count; index++) {
			training_data_pos.Add (training_data.Users [index], training_data.Items [index]);
		}


		MyMediaLite.ItemRecommendation.MostPopular recommender = new MyMediaLite.ItemRecommendation.MostPopular ();
		recommender.Feedback = training_data_pos;
		DateTime start_time = DateTime.Now;
		recommender.Train ();

		Console.Write ("Total Training time needed:");
		Console.WriteLine (((TimeSpan)(DateTime.Now - start_time)).TotalMilliseconds);
		Console.WriteLine ("Final results in this iteration:");
		var results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, validation_data, training_data, "VALIDATION ", false);
		results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, test_data, training_data,"TEST ",false);
		//}
	}

	private static void startItemKNN(string data){
		MyMediaLite.Data.Mapping user_mapping = new MyMediaLite.Data.Mapping ();
		MyMediaLite.Data.Mapping item_mapping = new MyMediaLite.Data.Mapping ();
		ITimedRatings all_data = readDataMapped (data, ref user_mapping, ref item_mapping);
		removeUserThreshold (ref all_data);

		Console.WriteLine("Start iteration Test ItemKNN");

		ITimedRatings validation_data = new TimedRatings (); // 10%
		ITimedRatings test_data = new TimedRatings (); // 20%
		ITimedRatings training_data = new TimedRatings (); // 70%
		readAndSplitData (all_data, ref test_data, ref training_data, ref validation_data);
		IPosOnlyFeedback training_data_pos = new PosOnlyFeedback<SparseBooleanMatrix> (); // 80%
		for (int index = 0; index < training_data.Users.Count; index++) {
			training_data_pos.Add (training_data.Users [index], training_data.Items [index]);
		}


		MyMediaLite.ItemRecommendation.ItemKNN recommender = new MyMediaLite.ItemRecommendation.ItemKNN ();
		recommender.Feedback = training_data_pos;
		DateTime start_time = DateTime.Now;
		recommender.Train ();

		Console.Write ("Total Training time needed:");
		Console.WriteLine (((TimeSpan)(DateTime.Now - start_time)).TotalMilliseconds);
		Console.WriteLine ("Final results in this iteration:");
		var results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, validation_data, training_data, "VALIDATION ", false);
		results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, test_data, training_data,"TEST ",false);
		//}
	}

	private static void startUserKNN(string data){
		MyMediaLite.Data.Mapping user_mapping = new MyMediaLite.Data.Mapping ();
		MyMediaLite.Data.Mapping item_mapping = new MyMediaLite.Data.Mapping ();
		ITimedRatings all_data = readDataMapped (data, ref user_mapping, ref item_mapping);
		removeUserThreshold (ref all_data);
		Console.WriteLine("Start iteration Test UserKNN");
		//for (int i = 0; i < 5; i++) {
		ITimedRatings validation_data = new TimedRatings (); // 10%
		ITimedRatings test_data = new TimedRatings (); // 20%
		ITimedRatings training_data = new TimedRatings (); // 70%
		readAndSplitData (all_data, ref test_data, ref training_data, ref validation_data);
		IPosOnlyFeedback training_data_pos = new PosOnlyFeedback<SparseBooleanMatrix> (); // 80%
		for (int index = 0; index < training_data.Users.Count; index++) {
			training_data_pos.Add (training_data.Users [index], training_data.Items [index]);
		}


		MyMediaLite.ItemRecommendation.UserKNN recommender = new MyMediaLite.ItemRecommendation.UserKNN ();
		recommender.K = 80;
		recommender.Q = 1;
		recommender.Weighted = false;
		recommender.Alpha = 0.5f;
		recommender.Correlation = MyMediaLite.Correlation.BinaryCorrelationType.Jaccard;
		recommender.Feedback = training_data_pos;
		DateTime start_time = DateTime.Now;
		recommender.Train ();

		Console.Write ("Total Training time needed:");
		Console.WriteLine (((TimeSpan)(DateTime.Now - start_time)).TotalMilliseconds);
		Console.WriteLine ("Final results in this iteration:");
		var results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, validation_data, training_data, "VALIDATION ", false);
		results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, test_data, training_data,"TEST ",false);
		//}
	}

	private static void startWRMF(ITimedRatings all_data){
		removeUserThreshold (ref all_data);
		Console.WriteLine("Start iteration Test WRMF");
		//for (int i = 0; i < 5; i++) {
		ITimedRatings validation_data = new TimedRatings (); // 10%
		ITimedRatings test_data = new TimedRatings (); // 20%
		ITimedRatings training_data = new TimedRatings (); // 70%
		readAndSplitData (all_data, ref test_data, ref training_data, ref validation_data);
		IPosOnlyFeedback training_data_pos = new PosOnlyFeedback<SparseBooleanMatrix> (); // 80%
		for (int index = 0; index < training_data.Users.Count; index++) {
			training_data_pos.Add (training_data.Users [index], training_data.Items [index]);
		}


		MyMediaLite.ItemRecommendation.WRMF recommender = new MyMediaLite.ItemRecommendation.WRMF ();
		recommender.Feedback = training_data_pos;
		DateTime start_time = DateTime.Now;
		recommender.Train ();

		Console.Write ("Total Training time needed:");
		Console.WriteLine (((TimeSpan)(DateTime.Now - start_time)).TotalMilliseconds);
		Console.WriteLine ("Final results in this iteration:");
		var results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, validation_data, training_data, "VALIDATION ", false);
		results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, test_data, training_data,"TEST ",false);
		//}
	}

	private static void startBPRMF(ITimedRatings all_data){
		removeUserThreshold (ref all_data);
		Console.WriteLine("Start iteration Test BPRMF");
		//for (int i = 0; i < 5; i++) {
		ITimedRatings validation_data = new TimedRatings (); // 10%
		ITimedRatings test_data = new TimedRatings (); // 20%
		ITimedRatings training_data = new TimedRatings (); // 70%
		readAndSplitData (all_data, ref test_data, ref training_data, ref validation_data);
		IPosOnlyFeedback training_data_pos = new PosOnlyFeedback<SparseBooleanMatrix> (); // 80%
		for (int index = 0; index < training_data.Users.Count; index++) {
			training_data_pos.Add (training_data.Users [index], training_data.Items [index]);
		}


		MyMediaLite.ItemRecommendation.BPRMF recommender = new MyMediaLite.ItemRecommendation.BPRMF ();
		recommender.Feedback = training_data_pos;
		DateTime start_time = DateTime.Now;
		recommender.Train ();

		Console.Write ("Total Training time needed:");
		Console.WriteLine (((TimeSpan)(DateTime.Now - start_time)).TotalMilliseconds);
		Console.WriteLine ("Final results in this iteration:");
		var results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, validation_data, training_data, "VALIDATION ", false);
		results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, test_data, training_data,"TEST ",false);
		//}
	}
		
	public static void removeUserThreshold(ref ITimedRatings all_data){
		IList<int> items_to_delete = new List<int> ();
		Dictionary<int,IList<DateTime>> itemsTimes = all_data.getTimesItemDict ();
		foreach (int item in all_data.AllItems) {
			if (itemsTimes [item].Count < 20) {
				items_to_delete.Add (item);
			}
		}
		foreach(int item in items_to_delete)
			all_data.RemoveItem (item);

		IList<int> user_to_delete = new List<int>();
		Dictionary<int,IList<int>> userItems = all_data.getItemsUserDict ();
		foreach (int user in all_data.AllUsers)
			if (userItems[user].Count < 20) {
				user_to_delete.Add (user);
			}
		foreach (int user in user_to_delete)
			all_data.RemoveUser (user);

		Console.Write(all_data.Statistics ());
		Console.Write ("Finished removing thresholds");
	}
		

	/// <summary>
	/// Gets string for subselecting all id's used from database
	/// </summary>
	/// <returns>The all identifiers string for database.</returns>
	public static string getAllIdsStringForDatabase(ITimedRatings ratings){

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

	public static Dictionary<int,int> getCategories(ITimedRatings ratings){
		TestMediaLite.DBConnect conn = new TestMediaLite.DBConnect (connection);
		string all_ids = getAllIdsStringForDatabase (ratings);
		Dictionary<int,int> venueCategoryMapper = new Dictionary<int,int>();
		//select suca.id, v.id_int from VENUE v inner join CATEGORY ca on(v.category_id = ca.id) inner join CATEGORY suca on(ca.parent_category = suca.foursquare_id)
		List<string>[] res = conn.Select(" select ca.id, v.id_int from VENUE v inner join CATEGORY ca on(v.category_id = ca.id) where v.id_int in " + all_ids ,2);
		List<string> ca_ids = res[0];
		List<string> venue_ids = res[1];
		int i = 0;
		foreach (string venue_id in venue_ids) {
			int v_id = int.Parse (venue_id);
			int ca_id = int.Parse(ca_ids[i]);
			venueCategoryMapper.Add (v_id, ca_id);
			i++;
		}
		return venueCategoryMapper;
	}

//	public static double startMuTuning(ITimedRatings all_data){
//
//		ITimedRatings validation_data = new TimedRatings (); // 10%
//		ITimedRatings test_data = new TimedRatings (); // 20%
//		ITimedRatings training_data = new TimedRatings (); // 70%
//		ITimedRatings sub_all_data = new TimedRatings();
//
//		getSubset (all_data, ref sub_all_data);
//		removeUserThreshold (ref sub_all_data);
//		readAndSplitData (sub_all_data, ref validation_data, ref test_data, ref training_data);
//		Dictionary<int,int> venueCategoryMapper = getCategories(sub_all_data);
//
//		int K = 100;
//		int user_count = sub_all_data.AllUsers.Count;
//		int location_count = sub_all_data.AllItems.Count ();
//		Console.WriteLine (location_count);
//		int category_count = venueCategoryMapper.Values.ToList ().Distinct ().Count();
//
//		double[,] U1 = new double[user_count,K];
//		double[,] U2 = new double[user_count,K];
//		double[,] U3 = new double[user_count,K];
//		double[,] U4 = new double[user_count,K];
//		double[,] L1 = new double[location_count,K];
//		double[,] CA1 = new double[category_count,K];
//		Dictionary<int,int> idMapperCategories = new Dictionary<int,int>();
//		Dictionary<int,int> idMapperLocations =new Dictionary<int,int>();
//		Dictionary<int,int> idMapperUser = new Dictionary<int,int>();
//
//
//		initMatrixNormal (sub_all_data.AllUsers, ref U3, ref idMapperUser, K);
//		initMatrixNormal (sub_all_data.AllUsers, ref U4, ref idMapperUser, K);
//		initMatrixNormal (sub_all_data.AllUsers, ref U1, ref idMapperUser, K);
//		initMatrixNormal (sub_all_data.AllUsers, ref U2, ref idMapperUser, K);
//		initMatrixNormal(sub_all_data.AllItems, ref L1,ref idMapperLocations, K);
//		initMatrixNormal(venueCategoryMapper.Values.ToList ().Distinct ().ToList(), ref CA1,ref idMapperCategories ,K);
//		Console.WriteLine (idMapperCategories.Keys.ToList ().Count);
//		for (int i = 0; i < idMapperCategories.Keys.ToList ().Count; i++) {
//			Console.Write (idMapperCategories.Keys.ToList () [i]);
//			Console.Write (", ");
//			Console.Write (idMapperCategories.Values.ToList () [i]);
//		}
//		List<MyMediaLite.Eval.ItemRecommendationEvaluationResults> result_list = new List<MyMediaLite.Eval.ItemRecommendationEvaluationResults> ();
//	
//		double mu = 0.1;
//		double best_mu = 0;
//		double best_mu_value = 0;
//		while (mu <= 1+0.01) { // +0.01 because of double rounding error
//			Console.WriteLine ("Start with mu = " + mu.ToString ());
//			WeatherItemRecommender recommender = new WeatherItemRecommender (U1, U2, U3, U4, CA1, L1,idMapperLocations, idMapperCategories, idMapperUser);
//			recommender.connection_string = connection;
//			recommender.Ratings = training_data;
//			recommender.Validation = validation_data;
//			recommender.weather_category_aware = true;
//			recommender.weather_aware = false;
//			recommender.max_iter = 250;
//			recommender.evaluation_at = 251;
//			recommender.beta = 0f;
//			recommender.mu = mu;
//			DateTime start_time = DateTime.Now;
//			recommender.Train ();
//			Console.Write ("Total Training time needed:");
//			Console.WriteLine (((TimeSpan)(DateTime.Now - start_time)).TotalMilliseconds);
//			Console.WriteLine ("Final results in this iteration:");
//			MyMediaLite.Eval.ItemRecommendationEvaluationResults results = MyMediaLite.Eval.ItemsWeatherItemRecommender.EvaluateTime (recommender, test_data, training_data);
//			foreach (var key in results.Keys)
//				Console.WriteLine ("{0}={1}", key, results [key]);
//			if (results ["prec@5"] > best_mu_value) {
//				best_mu = mu;
//				best_mu_value = results ["prec@5"];
//			}
//			Console.WriteLine ("Finished mu = " + mu.ToString ());
//			mu +=	0.1;
//
//		}
//		return best_mu;
//	}

		
	public static void totalTest(string data_file, int mode, int city, int iterations, string feature){
		
		ITimedRatings validation_data = new TimedRatings (); // 10%
		ITimedRatings test_data = new TimedRatings (); // 20%
		ITimedRatings training_data = new TimedRatings (); // 70%
		ITimedRatings all_data = readData (data_file);


		if (mode == 1){
//			Console.WriteLine ("Start beta tuning");
//			double beta = 0.1;
//			for (int i = 0; i < 5; i++) {
//				Console.WriteLine ("Start beta total iteration " + i.ToString ());
//				beta = startBetaTuning (all_data);
//				Console.WriteLine ("Finished beta total iteration " + i.ToString ());
//				Console.WriteLine ("Best beta = " + beta.ToString ());
//			}
//			Console.WriteLine ("End beta tuning");
		}
		if (mode == 2) {
			double beta = 0.2;
			Console.WriteLine ("Start geo base-line algo");
			startIterationTestNew (all_data, false, beta, 0f, city, iterations);
			Console.WriteLine ("End geo base-line algo");
		}
		if (mode == 3) {
			double beta = 0.2;
			Console.WriteLine ("Start geo weather aware algo");
			startIterationTest (all_data, true, beta , 0f, city, iterations);
			Console.WriteLine ("End geo base-line algo");
		}
		if (mode == 4) {
			double beta = 0.2;
			Console.WriteLine ("Start weather context aware algo");
			startIterationTestNew(all_data, true, beta, 0f, city, iterations, feature);
			Console.WriteLine ("End weather context aware algo");
		}
		if (mode == 5) {
			Console.WriteLine ("Start most popular algo");
			startMostPopular (all_data);
			Console.WriteLine ("End most popular algo");
		}
		if (mode == 6) {
			Console.WriteLine ("Start ItemKNN algo");
			startItemKNN (data_file);
			Console.WriteLine ("End ItemKNN algo");
		}if (mode == 7) {
			Console.WriteLine ("Start UserKNN algo");
			startUserKNN (data_file);
			Console.WriteLine ("End UserKNN algo");
		}if (mode == 8) {
			Console.WriteLine ("Start WRMF algo");
			startWRMF (all_data);
			Console.WriteLine ("End WRMF algo");
		}if (mode == 9) {
			Console.WriteLine ("Start BPRMF algo");
			startBPRMF (all_data);
			Console.WriteLine ("End BPRMF algo");
		}





	}

	public static void Main(string[] args)
	{

		//"SERVER=localhost;DATABASE=aoberegger;UID=aoberegger;PASSWORD=SSlamvxZ";
		//"SERVER=localhost;DATABASE=foursquare;UID=root;PASSWORD=mopuvake86";
		string data_file = args [0]; //"C:\\Users\\Entwickler\\studium\\Masterarbeit\\MyMediaLite\\examples\\csharp\\TestMediaLite\\data\\city9_all.data";
		connection = args [1];
		int mode = int.Parse(args[2]);
		int city = int.Parse(args[3]);
		int iterations = int.Parse (args [4]);
		string feature = args[5];
		totalTest(data_file, mode, city, iterations, feature);

	}
}
