using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;

namespace ProjectTemplate
{
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	[System.Web.Script.Services.ScriptService]

	public class ProjectServices : System.Web.Services.WebService
	{
		////////////////////////////////////////////////////////////////////////
		///replace the values of these variables with your database credentials
		////////////////////////////////////////////////////////////////////////
		private string dbID = "sprc2023team11";
		private string dbPass = "sprc2023team11";
		private string dbName = "sprc2023team11";
		////////////////////////////////////////////////////////////////////////
		
		////////////////////////////////////////////////////////////////////////
		///call this method anywhere that you need the connection string!
		////////////////////////////////////////////////////////////////////////
		private string getConString() {
			return "SERVER=107.180.1.16; PORT=3306; DATABASE=" + dbName+"; UID=" + dbID + "; PASSWORD=" + dbPass;
		}
		////////////////////////////////////////////////////////////////////////
		

		/////////////////////////////////////////////////////////////////////////
		//don't forget to include this decoration above each method that you want
		//to be exposed as a web service!
		[WebMethod(EnableSession = true)]
		/////////////////////////////////////////////////////////////////////////
		public string TestConnection()
		{
			try
			{
				string testQuery = "select * from users";

				////////////////////////////////////////////////////////////////////////
				///here's an example of using the getConString method!
				////////////////////////////////////////////////////////////////////////
				MySqlConnection con = new MySqlConnection(getConString());
				////////////////////////////////////////////////////////////////////////

				MySqlCommand cmd = new MySqlCommand(testQuery, con);
				MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
				DataTable table = new DataTable();
				adapter.Fill(table);
				return "Success!";
			}
			catch (Exception e)
			{
				return "Something went wrong, please check your credentials and db name and try again.  Error: "+e.Message;
			}
		}
		
		[WebMethod(EnableSession = true)] //NOTICE: gotta enable session on each individual method
		public bool LogOn(string email, string pass)
		{
			//we return this flag to tell them if they logged in or not
			bool success = false;
			
			string sqlSelect = "SELECT userID FROM users WHERE email=@emailValue and pword=@passValue";

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@emailValue", HttpUtility.UrlDecode(email));
			sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(pass));

			
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			DataTable sqlDt = new DataTable();
			sqlDa.Fill(sqlDt);
			
			if (sqlDt.Rows.Count > 0)
			{
				//if we found an account, store the id and admin status in the session
				//so we can check those values later on other method calls to see if they 
				//are 1) logged in at all, and 2) and admin or not
				Session["id"] = sqlDt.Rows[0]["id"];
				// Session["admin"] = sqlDt.Rows[0]["admin"];
				success = true;
				// call a function that can connect to database again and store user login time or any details 
				// into the loginstatus table
				int userid = int.Parse(Session["id"].ToString());
				UpdateLoginStatus(userid, "logged in");
			}
			//return the result!
			return success;
		}

		[WebMethod(EnableSession = true)]
		public bool LogOff()
		{
			//if they log off, then we remove the session.  That way, if they access
			//again later they have to log back on in order for their ID to be back
			//in the session!
			Session.Abandon();
			int userid = int.Parse(Session["id"].ToString());
			UpdateLoginStatus(userid,"logged off");
			return true;
		}
		
		public void UpdateLoginStatus(int id, string log)
		{
			AllowChange();

			string sqlSelect = "insert into login_status (id, time, log) values(@idValue, now(), @logValue);";
			
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
			
			sqlCommand.Parameters.AddWithValue("@idValue", id);
			sqlCommand.Parameters.AddWithValue("@logValue", log);

			sqlConnection.Open();
			//we're using a try/catch so that if the query errors out we can handle it gracefully
			//by closing the connection and moving on
			Console.WriteLine("Executing query...");
			try
			{
				sqlCommand.ExecuteNonQuery();
				Console.WriteLine("Query executed");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			sqlConnection.Close(); 
		}

		public void AllowChange()
		{
			string sqlSet = "set foreign_key_checks = 0;";
			
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSet, sqlConnection);
			
			sqlConnection.Open();
			
			try
			{
				sqlCommand.ExecuteNonQuery();
				Console.WriteLine("Foreign key query executed");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			sqlConnection.Close(); 
		}
		
		
		[WebMethod(EnableSession = true)]
		public Account[] StoreAccounts()
		{
			DataTable sqlDt = new DataTable("accounts");

			// string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
			string sqlSelect = "select userID, email, pword, fName, lName, userRole, experience, company from users;";

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

			//gonna use this to fill a data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//filling the data table
			sqlDa.Fill(sqlDt);
			
			List<Account> accounts = new List<Account>();
			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				accounts.Add(new Account
				{
					userId = sqlDt.Rows[i]["userID"].ToString(),
					email = sqlDt.Rows[i]["email"].ToString(),
					password = sqlDt.Rows[i]["pword"].ToString(),
					firstName = sqlDt.Rows[i]["fName"].ToString(),
					lastName = sqlDt.Rows[i]["lName"].ToString(),
					role = sqlDt.Rows[i]["userRole"].ToString(),
					experience = sqlDt.Rows[i]["experience"].ToString(),	
					company = sqlDt.Rows[i]["company"].ToString(),
				});
			}
			//convert the list of accounts to an array and return!
			return accounts.ToArray();
		}
		

		[WebMethod(EnableSession = true)]
		public void RequestAccount(string uid, string pass, string firstName, string lastName, string email, string company, string role, string year)
		{
			// string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
			//the only thing fancy about this query is SELECT LAST_INSERT_ID() at the end.  All that
			//does is tell mySql server to return the primary key of the last inserted row.
			string sqlSelect = "insert into users (userid, email, pword, fName, lName, userRole, phone, experience, company, industry, prompt, skills) " +
			                   "values(@idValue, @emailValue, @passValue, @fnameValue, @lnameValue, @roleValue, null, @experienceValue, @companyValue, " +
			                   "null, null, null); SELECT LAST_INSERT_ID();";
			
			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

			sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(uid));
			sqlCommand.Parameters.AddWithValue("@emailValue", HttpUtility.UrlDecode(email));
			sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(pass));
			sqlCommand.Parameters.AddWithValue("@fnameValue", HttpUtility.UrlDecode(firstName));
			sqlCommand.Parameters.AddWithValue("@lnameValue", HttpUtility.UrlDecode(lastName));
			sqlCommand.Parameters.AddWithValue("@companyValue", HttpUtility.UrlDecode(company));
			sqlCommand.Parameters.AddWithValue("@roleValue", HttpUtility.UrlDecode(role));
			sqlCommand.Parameters.AddWithValue("@experienceValue", HttpUtility.UrlDecode(year));

			sqlConnection.Open();
			//we're using a try/catch so that if the query errors out we can handle it gracefully
			//by closing the connection and moving on
			try
			{
				int accountID = Convert.ToInt32(sqlCommand.ExecuteScalar());
				//here, you could use this accountID for additional queries regarding
				//the requested account.  Really this is just an example to show you
				//a query where you get the primary key of the inserted row back from
				//the database!
			}
			catch (Exception e) {
			}
			sqlConnection.Close();
		}
	}
}
