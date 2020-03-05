using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using System.Data.SqlClient;
using System.Data.Sql;
using System.Data.SqlTypes;
using System.Data;
using System.Data.Common;
using System.Configuration;
using WebMatrix.Data;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;

namespace FDWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] {"INSTRUCTIONS:", "For Authorization endpoint, use: /api/values/{Username}/{Password} Ex: /api/values/admin/admin", "For Customer record endpoint, use: /api/values/{NoOfEmployees}/{tag}/{authToken} Ex: /api/values/1-10/Retail/mhbtz5B1l3cy95MtH245v11DouQ3UWdrmrDXnRXTNLM1LKwkYojXY11IfWby3BCc6Af0jShOi8DRIPq0DpojtCNxknvxKOGUPXyvM" };
        }

        // GET api/values/5/freetext
        [HttpGet("{NoOfEmployees}/{tag}/{authToken}")]
        public ActionResult<IEnumerable<string>> Get(string NoOfEmployees, string tag, string authToken)
        {
            string[] result = new string[1];
            if (authToken == "" || authToken == null)
            {
                result[0] = "No Authorization Token!";
            }
            else
            {
                DataTable dtt = new DataTable();
                DataTable dttAuthCheck = new DataTable();

                dttAuthCheck = ExecuteQuery("select * from Users where UserAccessToken = '" + authToken + "'", ConnectionString);
                
              
                if (dttAuthCheck.Rows.Count > 0)
                {
                    if (NoOfEmployees == "1-10")
                    {
                        dtt = ExecuteQuery("select * from Customers where CustomerTags = '" + tag + "' OR " + "NoOfEmployees BETWEEN 1 AND 10", ConnectionString);
                        result[0] = DataTableToJSONWithStringBuilder(dtt);
                    }
                    else if (NoOfEmployees == "11-50")
                    {
                        dtt = ExecuteQuery("select * from Customers where CustomerTags = '" + tag + "' OR " + "NoOfEmployees BETWEEN 11 AND 50", ConnectionString);
                        result[0] = DataTableToJSONWithStringBuilder(dtt);
                    }
                    else if (NoOfEmployees == ">50")
                    {
                        dtt = ExecuteQuery("select * from Customers where CustomerTags = '" + tag + "' OR " + "NoOfEmployees > 50", ConnectionString);
                        result[0] = DataTableToJSONWithStringBuilder(dtt);
                    }
                }
                else
                {
                    result[0] = "Invalid Authorization Token!";
                }
                
            }
            

            return result;
        }

        [HttpGet("{Username}/{Password}")]
        public ActionResult<IEnumerable<string>> Get(string Username , string Password)
        {
            string[] result = new string[1];
            DataTable dttAuthCheck = new DataTable();
            dttAuthCheck = ExecuteQuery("select * from Users WHERE UserName = '" + Username + "' AND Password ='" + Password + "'", ConnectionString);
            if (dttAuthCheck.Rows.Count > 0)
            {
                result[0] = HashValueWithRandomSalt(Username + DateTime.Now.ToLongTimeString());
                ExecuteNonQuery("UPDATE Users SET UserAccessToken = '" + result[0] + "' WHERE UserName = '" + Username + "' AND Password ='" + Password + "'", ConnectionString);
            }
            else {
                result[0] = "Invalid Username/Password!";
            }
            return result;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        public string ConnectionString = @"data source=WSMNL4SCORCH62\SQL2017;initial catalog=FDdb;integrated security=True;MultipleActiveResultSets=True;";

        private SqlConnection GetConnection()
        {
            SqlConnection connection = new SqlConnection(this.ConnectionString);
            if (connection.State != ConnectionState.Open)
                connection.Open();
            return connection;
        }

        public static DataTable ExecuteQuery(string commandText, string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))

            using (var command = new SqlCommand(commandText, connection))
            {
                DataTable dt = new DataTable();
                command.CommandType = CommandType.Text;
                SqlDataAdapter da = new SqlDataAdapter(command);
                da.Fill(dt);
                connection.Close();
                return dt;
            }
        }

        public static void ExecuteNonQuery(string commandText, string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))

            using (var command = new SqlCommand(commandText, connection))
            {
                command.CommandType = CommandType.Text;
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public static IEnumerable<T> DataTableToEnumerableObjectUsingJSONPropertyName<T>(DataTable dtbData)
        {
            Type typeOfObject = typeof(T);
            List<T> results = new List<T>();

            PropertyInfo[] properties = typeOfObject.GetProperties();

            foreach (DataRow dtr in dtbData.Rows)
            {
                object entry = Activator.CreateInstance(typeOfObject);

                foreach (PropertyInfo prop in properties)
                {
                    object value = dtr[prop.GetCustomAttributes<JsonPropertyAttribute>().Select(x => x.PropertyName).FirstOrDefault()];
                    prop.SetValue(entry, value);
                }
                results.Add((T)entry);
            }

            return results;
        }

        public string DataTableToJSONWithStringBuilder(DataTable table)
        {
            var JSONString = new StringBuilder();
            if (table.Rows.Count > 0)
            {
                JSONString.Append("[");
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    JSONString.Append("{");
                    for (int j = 0; j < table.Columns.Count; j++)
                    {
                        if (j < table.Columns.Count - 1)
                        {
                            JSONString.Append("\"" + table.Columns[j].ColumnName.ToString() + "\":" + "\"" + table.Rows[i][j].ToString() + "\",");
                        }
                        else if (j == table.Columns.Count - 1)
                        {
                            JSONString.Append("\"" + table.Columns[j].ColumnName.ToString() + "\":" + "\"" + table.Rows[i][j].ToString() + "\"");
                        }
                    }
                    if (i == table.Rows.Count - 1)
                    {
                        JSONString.Append("}");
                    }
                    else
                    {
                        JSONString.Append("},");
                    }
                }
                JSONString.Append("]");
            }
            return JSONString.ToString();
        }

        public static string HashValueWithRandomSalt(string value)
        {
            byte[] xBytes;


            byte[] salt = Generate();

            value = value + Convert.ToBase64String(salt);
            using (Rfc2898DeriveBytes hashy = new Rfc2898DeriveBytes(value, salt))
            {
                hashy.IterationCount = 555;

                xBytes = hashy.GetBytes(77);
            }
            StringBuilder sb = new StringBuilder();
            foreach (char c in Convert.ToBase64String(xBytes).Replace("/", "-"))
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString(); 
        }

        public static byte[] Generate()
        {
            DateTime timeNow = DateTime.Now;
            // Slightly randomizing max roll
            Double maxRoll = Math.Ceiling(timeNow.Second / 1.3) + DateTime.Now.Month + DateTime.Now.Hour;
            int rollCount = 0;

            // Slightly randomise the byte size with a minimum of 27 size
            byte[] xBytes = new byte[27 + (Int32)Math.Ceiling(timeNow.Second / 1.3) + 1];

            using (RNGCryptoServiceProvider _rnd = new RNGCryptoServiceProvider())
            {
                while (rollCount <= maxRoll)
                {
                    _rnd.GetBytes(xBytes);
                    rollCount += 1;
                }
            }

            return xBytes;
        }


    }
}
