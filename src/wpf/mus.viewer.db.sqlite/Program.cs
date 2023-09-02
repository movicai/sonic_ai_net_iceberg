using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mus.viewer.db.sqlite
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Entity;
    using System.Data.Entity.Core.Common;
    using System.Data.SQLite;
    using System.Data.SQLite.EF6;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class SQLiteConfiguration : DbConfiguration
    {
        public SQLiteConfiguration()
        {
            SetProviderFactory("System.Data.SQLite", SQLiteFactory.Instance);
            SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
            SetProviderServices("System.Data.SQLite", (DbProviderServices)SQLiteProviderFactory.Instance.GetService(typeof(DbProviderServices)));
        }
    }
    public class User
    {
        public int userid { get; set; }
        public string name { get; set; }
    }

    public class Device
    {
        [Key]
        public int seq { get; set; }
        public string name { get; set; }
        public string ipaddress { get; set; }
        public string subnet { get; set; }
        public string historyurl { get; set; }
        public string edgeurl { get; set; }
        public bool? iscollect { get; set; }

        public string desc  { get; set; }

    }

    public class MusDbContext : DbContext
    {
        public MusDbContext() : base(new SQLiteConnection()
        {
            ConnectionString = new SQLiteConnectionStringBuilder() { DataSource = System.IO.Path.Combine(System.Environment.CurrentDirectory, "SQLiteWithEF.db"), ForeignKeys = true }.ConnectionString
        }, true)
        {
        }

        //public DbSet<User> Users { get; set; }
        public DbSet<Device> Devices { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Device>().ToTable("Devices");
        }
    }

    //class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        using (var context = new MusDbContext())
    //        {
    //            // Create database if not exists
    //            //context.Database.CreateIfNotExists();

    //            // Add new User
    //            var user = new User() { name = "John Doe" };
    //            context.Users.Add(user);
    //            context.SaveChanges();

    //            // Query and print all users
    //            var users = context.Users.ToList();
    //            foreach (var u in users)
    //            {
    //                Console.WriteLine($"User Id: {u.userid}, User Name: {u.name}");
    //            }

    //            // Delete user
    //            context.Users.Remove(user);
    //            context.SaveChanges();
    //        }

    //        Console.ReadLine();
    //    }
    //}
}
