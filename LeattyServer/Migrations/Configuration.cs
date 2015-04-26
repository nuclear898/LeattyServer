using LeattyServer.Data;
using LeattyServer.DB.Models;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;
using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LeattyServer.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<LeattyContext> 
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
        }

        protected override void Seed(LeattyContext context)
        {
           
        }
    }
}
