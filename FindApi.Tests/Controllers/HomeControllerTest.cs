using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FindApi;
using FindApi.Controllers;

namespace FindApi.Tests.Controllers
{
    [TestClass]
    public class HomeControllerTest
    {
        [TestMethod]
        public void Index()
        {
            // Arrange
            HomeController controller = new HomeController();

            // Act
            FindResponse result = controller.Find(new QueryObject());

            // Assert
            Assert.IsNotNull(result);
        }
    }
}
