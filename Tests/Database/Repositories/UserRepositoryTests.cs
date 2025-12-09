using Database;
using Database.Entities;
using Database.Factories;
using Database.Repositories;
using Domain.Enums;
using Domain.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.Database.Repositories;

public class UserRepositoryTests
{
    private static IdentiverseDbContext CreateDb(SqliteConnection conn)
    {
        var options = new DbContextOptionsBuilder<IdentiverseDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new IdentiverseDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static (SqliteConnection conn, IdentiverseDbContext db, UserRepository repo) CreateRepo()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var db = CreateDb(conn);
        var factory = new UserFactory();
        var repo = new UserRepository(db, factory);
        return (conn, db, repo);
    }

    [Test]
    public async Task CreateAsync_Persists_User()
    {
        var scope = CreateRepo();
        try
        {
            var reg = new RegisterUserDto { Username = "u1", Email = "u1@x.com", Password = "Ignored", PersonId = null };
            var id = await scope.repo.RegisterUserAsync(reg, new byte[] {1,2,3}, new byte[] {4,5,6});
            var saved = await scope.db.Users.FirstOrDefaultAsync(u => u.Id == id);
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.Username, Is.EqualTo("u1"));
            Assert.That(saved.Email, Is.EqualTo("u1@x.com"));
            Assert.That(saved.Role, Is.EqualTo(UserRole.User));
            Assert.That(saved.PasswordHash, Is.Not.Empty);
            Assert.That(saved.PasswordSalt, Is.Not.Empty);
        }
        finally
        {
            scope.db.Dispose();
            scope.conn.Dispose();
        }
    }

    [Test]
    public async Task GetByIdAsync_Returns_User_When_Exists_Else_Null()
    {
        var scope = CreateRepo();
        try
        {
            scope.db.Users.Add(new User { Username = "a", Email = "a@a.com", Role = UserRole.User, PasswordHash = "h", PasswordSalt = "s" });
            await scope.db.SaveChangesAsync();

            var dto = await scope.repo.GetByIdAsync(1);
            Assert.That(dto, Is.Not.Null);
            var missing = await scope.repo.GetByIdAsync(999);
            Assert.That(missing, Is.Null);
        }
        finally
        {
            scope.db.Dispose();
            scope.conn.Dispose();
        }
    }

    [Test]
    public async Task GetByUsernameOrEmailAsync_Returns_User_For_Username_And_Email()
    {
        var scope = CreateRepo();
        try
        {
            scope.db.Users.Add(new User { Username = "bob", Email = "b@b.com", Role = UserRole.User, PasswordHash = "h", PasswordSalt = "s" });
            await scope.db.SaveChangesAsync();

            var authByUser = await scope.repo.GetAuthByUserNameOrEmailAsync("bob");
            Assert.That(authByUser, Is.Not.Null);
            var authByEmail = await scope.repo.GetAuthByUserNameOrEmailAsync("b@b.com");
            Assert.That(authByEmail, Is.Not.Null);
        }
        finally
        {
            scope.db.Dispose();
            scope.conn.Dispose();
        }
    }

    [Test]
    public async Task IsUsernameTakenAsync_Works()
    {
        var scope = CreateRepo();
        try
        {
            scope.db.Users.Add(new User { Username = "x", Email = "x@x.com", Role = UserRole.User, PasswordHash = "h", PasswordSalt = "s" });
            await scope.db.SaveChangesAsync();
            Assert.That(await scope.repo.IsUsernameTakenAsync("x"), Is.True);
            Assert.That(await scope.repo.IsUsernameTakenAsync("y"), Is.False);
        }
        finally
        {
            scope.db.Dispose();
            scope.conn.Dispose();
        }
    }

    [Test]
    public async Task IsEmailTakenAsync_Works()
    {
        var scope = CreateRepo();
        try
        {
            scope.db.Users.Add(new User { Username = "x2", Email = "x2@x.com", Role = UserRole.User, PasswordHash = "h", PasswordSalt = "s" });
            await scope.db.SaveChangesAsync();
            Assert.That(await scope.repo.IsEmailTakenAsync("x2@x.com"), Is.True);
            Assert.That(await scope.repo.IsEmailTakenAsync("no@x.com"), Is.False);
        }
        finally
        {
            scope.db.Dispose();
            scope.conn.Dispose();
        }
    }
}
