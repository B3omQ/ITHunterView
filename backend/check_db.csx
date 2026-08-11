using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory() + "/ITHunterview.WebAPI")
    .AddJsonFile("appsettings.Development.json");
var config = builder.Build();

var optionsBuilder = new DbContextOptionsBuilder<ITHunterviewContext>();
optionsBuilder.UseNpgsql(config.GetConnectionString("DefaultConnection") ?? config["ConnectionStrings:DefaultConnection"]);

using (var context = new ITHunterviewContext(optionsBuilder.Options))
{
    var count = context.InterviewQuestionBank.Count();
    Console.WriteLine("Total questions in DB: " + count);
}
