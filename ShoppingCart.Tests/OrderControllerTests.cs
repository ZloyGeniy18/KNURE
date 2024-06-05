using System;
using Moq;
using ShoppingCart.DataAccess.Repositories;
using ShoppingCart.DataAccess.ViewModels;
using ShoppingCart.Models;
using ShoppingCart.Tests.Datasets;
using ShoppingCart.Web.Areas.Admin.Controllers;
using System.Linq.Expressions;
using Xunit;
using ShoppingCart.Utility;
using Stripe;
using Stripe.TestHelpers;


namespace ShoppingCart.Tests
{
    public class OrderControllerTests
    {
        [Fact]
        public void OrderDetails_ReturnsCorrectOrderDetails()
        {
            // Arrange
            var orderId = 1;
            var orderHeader = new OrderHeader { Id = orderId };
            var orderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 1, OrderHeaderId = orderId },
                new OrderDetail { Id = 2, OrderHeaderId = orderId }
            };

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            unitOfWorkMock.Setup(uow => uow.OrderHeader.GetT(It.IsAny<Expression<Func<OrderHeader, bool>>>(), It.IsAny<string>()))
                .Returns(orderHeader);
            unitOfWorkMock.Setup(uow => uow.OrderDetail.GetAll(It.IsAny<string>()))
                .Returns(orderDetails);
            var controller = new OrderController(unitOfWorkMock.Object);

            // Act
            var result = controller.OrderDetails(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderHeader, result.OrderHeader);
            Assert.Equal(orderDetails, result.OrderDetails.ToList());
        }

        [Fact]
        public void SetToShipped_UpdatesOrderStatusToShipped()
        {
            // Arrange                  
            var orderHeader = new OrderHeader { Id = 1, Carrier = "Order", TrackingNumber = "12" };
            var orderVm = new OrderVM { OrderHeader = orderHeader };

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var orderHeaderRepositoryMock = new Mock<IOrderHeaderRepository>();
                        
            unitOfWorkMock.SetupGet(uow => uow.OrderHeader).Returns(orderHeaderRepositoryMock.Object);
            orderHeaderRepositoryMock.Setup(repo => repo.GetT(It.IsAny<Expression<Func<OrderHeader, bool>>>(), null))
                                     .Returns(orderHeader);

            var controller = new OrderController(unitOfWorkMock.Object);

            // Act
            controller.SetToShipped(orderVm);

            // Assert
            orderHeaderRepositoryMock.Verify(repo => repo.Update(It.Is<OrderHeader>(oh =>
                oh.Id == orderHeader.Id &&
                oh.Carrier == orderHeader.Carrier &&
                oh.TrackingNumber == orderHeader.TrackingNumber &&
                oh.OrderStatus == OrderStatus.StatusShipped)), Times.Once);
            unitOfWorkMock.Verify(uow => uow.Save(), Times.Once);

        }
             
        [Fact]
        public void SetToCancelOrder_PaymentNotApproved()
        {
            // Arrange
            var orderVm = new OrderVM { OrderHeader = new OrderHeader { Id = 1, PaymentStatus = PaymentStatus.StatusPending } };
            var orderHeader = new OrderHeader { Id = 1, PaymentStatus = PaymentStatus.StatusPending };

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            unitOfWorkMock.Setup(uow => uow.OrderHeader.GetT(It.IsAny<Expression<Func<OrderHeader, bool>>>(), null)).Returns(orderHeader);

            var controller = new OrderController(unitOfWorkMock.Object);

            // Act
            controller.SetToCancelOrder(orderVm);

            // Assert
            unitOfWorkMock.Verify(uow => uow.OrderHeader.UpdateStatus(orderVm.OrderHeader.Id, OrderStatus.StatusCancelled, null), Times.Once);
            unitOfWorkMock.Verify(uow => uow.Save(), Times.Once);

        }
    }
}
