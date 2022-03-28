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

            WalletModel wm = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    var query = @"Select * from Wallet where AccountId = "+input.Account+";";
                    SqlCommand command = new SqlCommand(query, connection);
                    var reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        wm = new WalletModel()
                        {
                            WalletId = Convert.ToInt32(reader["AccountId"]),
                            BalanceAmt = Convert.ToDecimal(reader["BalanceAmt"])
                        };
                    }
                }

                if(wm != null)
                {
                    if(input.Direction == "Credit" && input.Amount > 0)
                    {
                        // Perform credit operation (not in requirement)
                    }
                    else if (input.Direction == "Debit" && wm.BalanceAmt - input.Amount > 0)
                    {
                        try
                        {
                            using var connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString"));
                            connection.Open();

                            // update wallet
                            decimal balanceWalletAmt = wm.BalanceAmt - Convert.ToDecimal(input.Amount);
                            wm.BalanceAmt = balanceWalletAmt;
                            var updateWalletQuery = $"UPDATE [Wallet] Set BalanceAmt = " + balanceWalletAmt + " WHERE AccountId = " + input.Account;
                            SqlCommand updateCommand = new SqlCommand(updateWalletQuery, connection);
                            updateCommand.ExecuteNonQuery();

                            // insert transaction record
                            var insertTransactionQuery = $"INSERT INTO [Transaction] (TransactionId, Amount,Direction,AccountId) VALUES('{input.Id}', '{input.Amount}' , '{input.Direction}', '{input.Account}')";
                            SqlCommand insertCommand = new SqlCommand(insertTransactionQuery, connection);
                            insertCommand.ExecuteNonQuery();
                            
                            isTransactionSuccessful = true;
                        }
                        catch (Exception e)
                        {
                            log.LogError(e.ToString());
                            return new BadRequestResult();
                        }
                    }
                    else
                    {
                        return new OkObjectResult("Account exist, but balance insufficient! ");
                    }
                }
                else
                {
                    return new OkObjectResult("Account does not exist!");
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
                return new BadRequestResult();
            }

            string responseMessage = isTransactionSuccessful
                    ? "This transaction has  completed successfully and updated wallet amount is "+wm.BalanceAmt : "Transaction has failed due to insufficient balance";

            return new OkObjectResult(responseMessage);
        }
    }
}
