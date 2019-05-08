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
using System.Text;
using System.Diagnostics;
using System.IO;
//Add MySql Library
using MySql.Data.MySqlClient;


namespace TestMediaLite
{
	class DBConnect
	{
		private MySqlConnection connection;
		private string server;
		private string database;
		private string uid;
		private string password;

		//Constructor
		public DBConnect(string connection_string)
		{
			Initialize(connection_string);
		}

		//Initialize values
		private void Initialize(string connection_string)
		{
			server = "localhost";
			database = "aoberegger";
			uid = "aoberegger";
			password = "SSlamvxZ";

//			server = "localhost";
//			database = "foursquare";
//			uid = "root";
//			password = "changepw";
			string connectionString;

			connectionString = connection_string;

			connection = new MySqlConnection(connectionString);
		}


		//open connection to database
		private bool OpenConnection()
		{
			try
			{
				connection.Open();
				return true;
			}
			catch (MySqlException ex)
			{
				//When handling errors, you can your application's response based on the error number.
				//The two most common error numbers when connecting are as follows:
				//0: Cannot connect to server.
				//1045: Invalid user name and/or password.
				switch (ex.Number)
				{
				case 0:
					Console.WriteLine("Cannot connect to server.  Contact administrator");
					break;

				case 1045:
					Console.WriteLine("Invalid username/password, please try again");
					break;
				}
				return false;
			}
		}

		//Close connection
		private bool CloseConnection()
		{
			try
			{
				connection.Close();
				return true;
			}
			catch (MySqlException ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}
		}

		//Insert statement
		public void Insert()
		{
			string query = "INSERT INTO tableinfo (name, age) VALUES('John Smith', '33')";

			//open connection
			if (this.OpenConnection() == true)
			{
				//create command and assign the query and connection from the constructor
				MySqlCommand cmd = new MySqlCommand(query, connection);

				//Execute command
				cmd.ExecuteNonQuery();

				//close connection
				this.CloseConnection();
			}
		}

		//Update statement
		public void Update()
		{
			string query = "UPDATE tableinfo SET name='Joe', age='22' WHERE name='John Smith'";

			//Open connection
			if (this.OpenConnection() == true)
			{
				//create mysql command
				MySqlCommand cmd = new MySqlCommand();
				//Assign the query using CommandText
				cmd.CommandText = query;
				//Assign the connection using Connection
				cmd.Connection = connection;

				//Execute query
				cmd.ExecuteNonQuery();

				//close connection
				this.CloseConnection();
			}
		}

		//Delete statement
		public void Delete()
		{
			string query = "DELETE FROM tableinfo WHERE name='John Smith'";

			if (this.OpenConnection() == true)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.ExecuteNonQuery();
				this.CloseConnection();
			}
		}

		//Select statement
		public List<string>[] Select(string query, int rows)
		{
			//Create a list to store the result
			List<string>[] list = new List<string>[rows];
			for (int i = 0; i < rows; i++) {
				list [i] = new List<string> ();
			}

			if (this.OpenConnection() == true)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.CommandTimeout = 0;
				MySqlDataReader dataReader = cmd.ExecuteReader();
				while (dataReader.Read())
				{
					for (int i = 0; i < rows; i++) {
						list[i].Add(dataReader[i].ToString());
					}
				}
				dataReader.Close();
				this.CloseConnection();
				return list;
			}
			else
			{
				return list;
			}
		}

		//Count statement
		public int Count()
		{
			string query = "SELECT Count(*) FROM tableinfo";
			int Count = -1;

			//Open Connection
			if (this.OpenConnection() == true)
			{
				//Create Mysql Command
				MySqlCommand cmd = new MySqlCommand(query, connection);

				//ExecuteScalar will return one value
				Count = int.Parse(cmd.ExecuteScalar()+"");

				//close Connection
				this.CloseConnection();

				return Count;
			}
			else
			{
				return Count;
			}
		}

		//Backup
		public void Backup()
		{
			try
			{
				DateTime Time = DateTime.Now;
				int year = Time.Year;
				int month = Time.Month;
				int day = Time.Day;
				int hour = Time.Hour;
				int minute = Time.Minute;
				int second = Time.Second;
				int millisecond = Time.Millisecond;

				//Save file to C:\ with the current date as a filename
				string path;
				path = "C:\\" + year + "-" + month + "-" + day + "-" + hour + "-" + minute + "-" + second + "-" + millisecond + ".sql";
				StreamWriter file = new StreamWriter(path);


				ProcessStartInfo psi = new ProcessStartInfo();
				psi.FileName = "mysqldump";
				psi.RedirectStandardInput = false;
				psi.RedirectStandardOutput = true;
				psi.Arguments = string.Format(@"-u{0} -p{1} -h{2} {3}", uid, password, server, database);
				psi.UseShellExecute = false;

				Process process = Process.Start(psi);

				string output;
				output = process.StandardOutput.ReadToEnd();
				file.WriteLine(output);
				process.WaitForExit();
				file.Close();
				process.Close();
			}
			catch (IOException ex)
			{
				Console.WriteLine("Error , unable to backup!");
			}
		}

		//Restore
		public void Restore()
		{
			try
			{
				//Read file from C:\
				string path;
				path = "C:\\MySqlBackup.sql";
				StreamReader file = new StreamReader(path);
				string input = file.ReadToEnd();
				file.Close();


				ProcessStartInfo psi = new ProcessStartInfo();
				psi.FileName = "mysql";
				psi.RedirectStandardInput = true;
				psi.RedirectStandardOutput = false;
				psi.Arguments = string.Format(@"-u{0} -p{1} -h{2} {3}", uid, password, server, database);
				psi.UseShellExecute = false;


				Process process = Process.Start(psi);
				process.StandardInput.WriteLine(input);
				process.StandardInput.Close();
				process.WaitForExit();
				process.Close();
			}
			catch (IOException ex)
			{
				Console.WriteLine("Error , unable to Restore!");
			}
		}
	}
}

