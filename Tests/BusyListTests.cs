using System;
using System.Linq;

using NUnit.Framework;

using TodoList.Business.BusyList;


namespace TodoList.Tests
{
    [TestFixture]
    public class BusyListTests
    {
        [SetUp]
        public void CreateWorkItems()
        {
            var (db, admin, password) = AuthenticationTests.CreateDefaultDatabase();
            var userService = new Business.Authentification.Service(db);

            _todoListMgr = new Service(db, userService);
            _todoListMgr.Initialize(admin, userService.Authenticate(admin.Login, password));
            
            var builtInCat = db.Categories.Create();
            builtInCat.OwningUserName = null; // what's make them built-in
            builtInCat.Name = "Food shopping";
            db.Categories.Upsert(builtInCat);

            builtInCat = db.Categories.Create();
            builtInCat.OwningUserName = null; // what's make them built-in
            builtInCat.Name = "Hobby";
            db.Categories.Upsert(builtInCat);

            builtInCat = db.Categories.Create();
            builtInCat.OwningUserName = null; // what's make them built-in
            builtInCat.Name = "Professional Work";
            db.Categories.Upsert(builtInCat);
        }
        Service _todoListMgr;

        [TestCase(null, TestName = "1) Check existing default categories")]
        public void CheckExistingDefaultCategories(object _)
        {
            var builtInCats = _todoListMgr.GetAllCategories();
            Assert.AreEqual(builtInCats.Count, 3);
            Assert.IsTrue(builtInCats.Any(c => c.Name == "Food shopping"));
            Assert.IsTrue(builtInCats.Any(c => c.Name == "Hobby"));
            Assert.IsTrue(builtInCats.Any(c => c.Name == "Professional Work"));
            Assert.IsTrue(builtInCats.All(c => c.IsBuiltIn));
        }

        [TestCase(null, TestName = "2) Add remove category")]
        public void AddRemoveCategory(object _)
        {
            var cat = _todoListMgr.NewCategory();
            cat.Name = "test";
            cat.Save();

            var builtInCats = _todoListMgr.GetAllCategories();
            Assert.AreEqual(builtInCats.Count, 4);
            Assert.IsTrue(builtInCats.Any(c => c.Name == "test"));

            _todoListMgr.Delete(cat);

            builtInCats = _todoListMgr.GetAllCategories();
            Assert.AreEqual(builtInCats.Count, 3);
            Assert.IsTrue(builtInCats.All(c => c.Name != "test"));
        }

        [TestCase(null, TestName = "3) Add/Remove work items")]
        public void AddRemoveWorkItems(object _)
        {
            var item = _todoListMgr.NewWorkItem();
            item.Title = "Do Test";
            item.Description = "I should have use EntityFramework directly... it seems better than last time i use it (10 years ago)";
            item.Save();

            var cat = _todoListMgr.NewCategory();
            cat.Name = "Home work";
            cat.Save();

            var stdCat = _todoListMgr.GetAllCategories().Single(cat => cat.Name == "Professional Work");

            _todoListMgr.AddCategoryTo(cat, item);
            _todoListMgr.AddCategoryTo(stdCat, item);
            
            Assert.That(item.Categories.Count == 2);
            Assert.That(item.Categories.Any(c => c.Id == cat.Id));
            Assert.That(item.Categories.Any(c => c.Id == stdCat.Id));

            _todoListMgr.RemoveCategoryFrom(cat, item);
            _todoListMgr.RemoveCategoryFrom(stdCat, item);

            Assert.That(item.Categories.Count == 0);
        }
    }
}
