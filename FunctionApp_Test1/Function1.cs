using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace FunctionApp_Test1
{
    public static class Function1
    {
        [FunctionName("PerformTransaction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            bool isTransactionSuccessful = false;
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic input = JsonConvert.DeserializeObject<TransactionModel>(requestBody);

            try
            {
                WalletModel wm = null;
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    var query = @"Select * from Wallet where WalletId = "+input.AccountId+";";
                    SqlCommand command = new SqlCommand(query, connection);
                    var reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        wm = new WalletModel()
                        {
                            WalletId = (int)reader["WalletId"],
                            BalanceAmt = (decimal)reader["BalanceAmt"]
                        };
                    }
                }

                if(wm != null && input.Direction == "Debit" && wm.BalanceAmt - input.Amount > 0)
                {
                    try
                    {
                        using var connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString"));
                        connection.Open();
                        if (String.IsNullOrEmpty(input.TransactionId))
                        {
                            var query = $"INSERT INTO [Transaction] (TransactionId, Amount,Direction,AccountId) VALUES('{input.Id}', '{input.Amount}' , '{input.Direction}', '{input.AccountId}')";
                            SqlCommand command = new SqlCommand(query, connection);
                            command.ExecuteNonQuery();
                            isTransactionSuccessful = true;
                        }
                    }
                    catch (Exception e)
                    {
                        log.LogError(e.ToString());
                        return new BadRequestResult();
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
                return new BadRequestResult();
            }

            string responseMessage = isTransactionSuccessful
                    ? "This transaction has  completed successfully" : "Transaction has failed due to insufficient balance";

            return new OkObjectResult(responseMessage);
        }
    }
}
