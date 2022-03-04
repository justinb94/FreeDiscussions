using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;
using System.Reflection;
using System.Linq;

namespace FreeDiscussions.Plugins.TXTGroups
{

    public class PostReadDbContext : DbContext
    {
        public DbSet<PostReadItem> Items { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=history.db", options =>
            {
                options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
            });
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map table names
            modelBuilder.Entity<PostReadItem>().ToTable("Items");
            modelBuilder.Entity<PostReadItem>(entity =>
            {
                entity.HasKey(nameof(PostReadItem.Newsgroup), nameof(PostReadItem.MessageId));
                entity.Property(e => e.Newsgroup);
                entity.Property(e => e.MessageId);
                entity.Property(e => e.Date);
            });
            base.OnModelCreating(modelBuilder);
        }
    }

    public class PostReadItem
    {
        public string Newsgroup { get; set; }
        public string MessageId { get; set; }
        public DateTime Date { get; set; }
    }

    class PostRead
    {
        private static PostReadDbContext _Context;

        private static PostReadDbContext GetContext()
        {
            if (_Context == default(PostReadDbContext))
            {
                _Context = new PostReadDbContext();
                _Context.Database.EnsureCreated();
            }
            return _Context;

        }

        public static bool IsRead(string newsgroup, string messageId)
        {
            var context = GetContext();
            
            return context.Items.Any(x => x.Newsgroup.Equals(newsgroup) && x.MessageId.Equals(messageId));
        }

        public static void MarkAsRead(string newsgroup, string messageId)
        {
            var context = GetContext();
            if (IsRead(newsgroup, messageId)) return;
            context.Items.Add(new PostReadItem
            {
                Newsgroup = newsgroup,
                MessageId = messageId,
                Date = DateTime.Now
            });
            context.SaveChanges();
        }
    }
}
