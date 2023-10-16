﻿using DentalLabManagement.BusinessTier.Constants;
using DentalLabManagement.BusinessTier.Enums;
using DentalLabManagement.BusinessTier.Payload.Order;
using DentalLabManagement.BusinessTier.Payload.Product;
using DentalLabManagement.BusinessTier.Payload.TeethPosition;
using DentalLabManagement.BusinessTier.Services.Interfaces;
using DentalLabManagement.BusinessTier.Utils;
using DentalLabManagement.DataTier.Models;
using DentalLabManagement.DataTier.Paginate;
using DentalLabManagement.DataTier.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DentalLabManagement.BusinessTier.Services.Implements
{
    public class OrderService : BaseService<OrderService>, IOrderService
    {
        public OrderService(IUnitOfWork<DentalLabManagementContext> unitOfWork, ILogger<OrderService> logger) : base(unitOfWork, logger)
        {

        }

        public async Task<CreateOrderResponse> CreateNewOrder(CreateOrderRequest createOrderRequest)
        {
            DateTime currentTime = TimeUtils.GetCurrentSEATime();

            Dental dental = await _unitOfWork.GetRepository<Dental>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(createOrderRequest.DentalId));
            if (dental == null) throw new BadHttpRequestException(MessageConstant.Dental.DentalNotFoundMessage);

            Order newOrder = new Order()
            {
                DentalId = dental.Id,
                DentistName = createOrderRequest.DentistName,
                DentistNote = createOrderRequest.DentistNote,
                PatientName = createOrderRequest.PatientName,
                PatientGender = createOrderRequest.PatientGender.GetDescriptionFromEnum(),
                Status = OrderStatus.New.GetDescriptionFromEnum(),
                Mode = createOrderRequest.Mode.GetDescriptionFromEnum(),
                TotalAmount = createOrderRequest.TotalAmount,
                Discount = createOrderRequest.Discount,
                FinalAmount = createOrderRequest.TotalAmount - createOrderRequest.Discount,
                CreatedDate = currentTime,
            };

            await _unitOfWork.GetRepository<Order>().InsertAsync(newOrder);
            await _unitOfWork.CommitAsync();

            newOrder.InvoiceId = "E" + (dental.Id * 10000 + newOrder.Id).ToString("D5");
            await _unitOfWork.CommitAsync();
            int count = 0;

            List<OrderItem> orderItems = new List<OrderItem>();
            createOrderRequest.ProductsList.ForEach(product =>
            {
                double totalProductAmount = product.SellingPrice * product.Quantity;
                orderItems.Add(new OrderItem()
                {
                    OrderId = newOrder.Id,
                    ProductId = product.ProductId,
                    TeethPositionId = product.TeethPositionId,
                    SellingPrice = product.SellingPrice,
                    Quantity = product.Quantity,
                    Note = product.Note,
                    TotalAmount = totalProductAmount,
                });
                count++;
            });

            newOrder.TeethQuantity = count;
            await _unitOfWork.GetRepository<OrderItem>().InsertRangeAsync(orderItems);
            await _unitOfWork.CommitAsync();
            return new CreateOrderResponse(newOrder.Id, newOrder.InvoiceId, dental.Name,
                newOrder.DentistName, newOrder.DentistNote, newOrder.PatientName,
                EnumUtil.ParseEnum<PatientGender>(newOrder.PatientGender),
                EnumUtil.ParseEnum<OrderStatus>(newOrder.Status),
                EnumUtil.ParseEnum<OrderMode>(newOrder.Mode), newOrder.TeethQuantity,
                newOrder.TotalAmount, newOrder.Discount, newOrder.FinalAmount, newOrder.CreatedDate);

        }

        public async Task<IPaginate<GetOrderDetailResponse>> GetOrders(
            string? InvoiceId, OrderMode? mode, OrderStatus? status, int page, int size)
        {
            InvoiceId = InvoiceId?.Trim().ToLower();

            var orderList = await _unitOfWork.GetRepository<Order>().GetPagingListAsync(
                selector: x => new GetOrderDetailResponse()
                {
                    Id = x.Id,
                    InvoiceId = x.InvoiceId,
                    DentalName = x.Dental.Name,
                    DentistName = x.DentistName,
                    DentistNote = x.DentistNote,
                    PatientName = x.PatientName,
                    PatientGender = EnumUtil.ParseEnum<PatientGender>(x.PatientGender),
                    Status = EnumUtil.ParseEnum<OrderStatus>(x.Status),
                    Mode = EnumUtil.ParseEnum<OrderMode>(x.Mode),
                    TeethQuantity = x.TeethQuantity,
                    TotalAmount = x.TotalAmount,
                    Discount = x.Discount,
                    FinalAmount = x.FinalAmount,
                    CreatedDate = x.CreatedDate
                },
                predicate: string.IsNullOrEmpty(InvoiceId) && (mode == null) && (status == null)
                ? x => true
                : ((status == null && mode == null)
                    ? x => x.InvoiceId.Contains(InvoiceId)
                    : ((status == null)
                        ? x => x.InvoiceId.Contains(InvoiceId) && x.Mode.Equals(mode.GetDescriptionFromEnum())
                        : ((mode == null)
                            ? x => x.InvoiceId.Contains(InvoiceId) && x.Status.Equals(status.GetDescriptionFromEnum())
                            : x => x.InvoiceId.Contains(InvoiceId) && x.Mode.Equals(mode.GetDescriptionFromEnum())
                                && x.Status.Equals(status.GetDescriptionFromEnum())))),
                orderBy: x => x.OrderBy(x => x.InvoiceId),
                page: page,
                size: size
            );

            foreach (var order in orderList.Items)
            {
                order.ToothList = (List<OrderItemResponse>)await _unitOfWork.GetRepository<OrderItem>()
                    .GetListAsync(
                        selector: x => new OrderItemResponse()
                        {
                            Id = x.Id,
                            OrderId = x.OrderId,
                            Product = new ProductResponse()
                            {
                                Id = x.ProductId,
                                Name = x.Product.Name,
                                Description = x.Product.Description,
                                CostPrice = x.Product.CostPrice,
                                CategoryId = x.Product.CategoryId
                            },
                            TeethPosition = new TeethPositionResponse()
                            {
                                Id = x.TeethPositionId,
                                ToothArch = EnumUtil.ParseEnum<ToothArch>(x.TeethPosition.ToothArch.ToString()),
                                PositionName = x.TeethPosition.PositionName,
                                Description = x.TeethPosition.Description
                            },
                            Note = x.Note,
                            SellingPrice = x.SellingPrice,
                            Quantity = x.Quantity,
                            TotalAmount = x.TotalAmount
                        },
                        predicate: x => x.OrderId.Equals(order.Id)
                    );
            }
            return orderList;
        }


        public async Task<GetOrderDetailResponse> GetOrderTeethDetail(int id)
        {
            if (id < 1) throw new BadHttpRequestException(MessageConstant.Order.EmptyOrderIdMessage);
            Order order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(id));
            if (order == null) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFoundMessage);
            Dental dental = await _unitOfWork.GetRepository<Dental>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(order.DentalId));
            if (dental == null) throw new BadHttpRequestException(MessageConstant.Dental.DentalNotFoundMessage);

            GetOrderDetailResponse orderItemResponse = new GetOrderDetailResponse();
            orderItemResponse.Id = order.Id;
            orderItemResponse.InvoiceId = order.InvoiceId;
            orderItemResponse.DentalName = await _unitOfWork.GetRepository<Dental>().SingleOrDefaultAsync(
                selector: x => x.Name, predicate: x => x.Id.Equals(order.DentalId));
            orderItemResponse.DentistName = order.DentistName;
            orderItemResponse.DentistNote = order.DentistNote;
            orderItemResponse.PatientName = order.PatientName;
            orderItemResponse.PatientGender = EnumUtil.ParseEnum<PatientGender>(order.PatientGender);
            orderItemResponse.Status = EnumUtil.ParseEnum<OrderStatus>(order.Status);
            orderItemResponse.Mode = EnumUtil.ParseEnum<OrderMode>(order.Mode);
            orderItemResponse.TeethQuantity = order.TeethQuantity;
            orderItemResponse.TotalAmount = order.TotalAmount;
            orderItemResponse.Discount = order.Discount;
            orderItemResponse.FinalAmount = order.FinalAmount;
            orderItemResponse.CreatedDate = order.CreatedDate;


            orderItemResponse.ToothList = (List<OrderItemResponse>)await _unitOfWork.GetRepository<OrderItem>()
                .GetListAsync(
                    selector: x => new OrderItemResponse()
                    {
                        Id = x.Id,
                        OrderId = x.OrderId,
                        Product = new ProductResponse()
                        {
                            Id = x.ProductId,
                            Name = x.Product.Name,
                            Description = x.Product.Description,
                            CostPrice = x.Product.CostPrice,
                            CategoryId = x.Product.CategoryId
                        },
                        TeethPosition = new TeethPositionResponse()
                        {
                            Id = x.TeethPositionId,
                            ToothArch = EnumUtil.ParseEnum<ToothArch>(x.TeethPosition.ToothArch.ToString()),
                            PositionName = x.TeethPosition.PositionName,
                            Description = x.TeethPosition.Description
                        },
                        Note = x.Note,
                        SellingPrice = x.SellingPrice,
                        Quantity = x.Quantity,
                        TotalAmount = x.TotalAmount
                    },
                    predicate: x => x.OrderId.Equals(id)
                );

            return orderItemResponse;

        }

        public async Task<UpdateOrderResponse> UpdateOrder(int orderId, UpdateOrderRequest updateOrderRequest)
        {
            if (orderId < 1) throw new BadHttpRequestException(MessageConstant.Order.EmptyOrderIdMessage);

            DateTime currentTime = TimeUtils.GetCurrentSEATime();

            Order updateOrder = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(orderId)
                //include: y => y.Include(a => a.OrderItems).ThenInclude(b => b.Product)
                );
            ICollection<OrderItem> orderItems = await _unitOfWork.GetRepository<OrderItem>().GetListAsync(
                predicate: x => x.OrderId.Equals(orderId),
                include: y => y.Include(a => a.Product)
                );

            if (updateOrder == null) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFoundMessage);

            Account account = await _unitOfWork.GetRepository<Account>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(updateOrderRequest.UpdatedBy));

            if (account == null) throw new BadHttpRequestException(MessageConstant.Account.AccountNotFoundMessage);
            OrderStatus status = updateOrderRequest.Status;
            

            switch (status)
            {

                case OrderStatus.Producing:

                    if (updateOrder.Status.Equals(OrderStatus.Producing.GetDescriptionFromEnum()))
                        return new UpdateOrderResponse(EnumUtil.ParseEnum<OrderStatus>(updateOrder.Status),
                            account.FullName, updateOrder.UpdatedAt, MessageConstant.Order.ProducingStatusMessage);

                    List<OrderItemStage> orderItemStageList = new List<OrderItemStage>();
                    foreach (var item in orderItems)
                    {
                        ICollection<GroupStage> stageList = await _unitOfWork.GetRepository<GroupStage>().GetListAsync(
                            predicate: p => p.CategoryId.Equals(item.Product.CategoryId),
                            include: x => x.Include(c => c.ProductStage)
                          );

                        foreach (var itemStage in stageList)
                        {
                            OrderItemStage newStage = new OrderItemStage()
                            {
                                OrderItemId = item.Id,
                                IndexStage = itemStage.ProductStage.IndexStage,
                                StageName = itemStage.ProductStage.Name,
                                Description = itemStage.ProductStage.Description,
                                ExecutionTime = itemStage.ProductStage.ExecutionTime,
                                Status = OrderItemStageStatus.Pending.GetDescriptionFromEnum(),
                                StartDate = currentTime,
                            };
                            orderItemStageList.Add(newStage);
                        }

                    }

                    await _unitOfWork.GetRepository<OrderItemStage>().InsertRangeAsync(orderItemStageList);
                    updateOrder.Status = OrderStatus.Producing.GetDescriptionFromEnum();
                    updateOrder.UpdatedBy = account.Id;
                    updateOrder.UpdatedAt = currentTime;
                    _unitOfWork.GetRepository<Order>().UpdateAsync(updateOrder);
                    
                    bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
                    if (!isSuccessful) throw new BadHttpRequestException(MessageConstant.Order.UpdateStatusFailedMessage);
                    
                    return new UpdateOrderResponse(EnumUtil.ParseEnum<OrderStatus>(updateOrder.Status),
                        account.FullName, updateOrder.UpdatedAt, MessageConstant.Order.ProducingStatusMessage);                  

                case OrderStatus.Completed:
                    updateOrder.Status = updateOrderRequest.Status.GetDescriptionFromEnum();
                    updateOrder.UpdatedBy = account.Id;
                    updateOrder.UpdatedAt = currentTime;
                    _unitOfWork.GetRepository<Order>().UpdateAsync(updateOrder);
                    isSuccessful = await _unitOfWork.CommitAsync() > 0;
                    if (!isSuccessful) throw new BadHttpRequestException(MessageConstant.Order.UpdateStatusFailedMessage);
                    return new UpdateOrderResponse(EnumUtil.ParseEnum<OrderStatus>(updateOrder.Status),
                        account.FullName, updateOrder.UpdatedAt, MessageConstant.Order.CompletedStatusMessage);
                    
                case OrderStatus.Canceled:
                    updateOrder.Status = updateOrderRequest.Status.GetDescriptionFromEnum();
                    updateOrder.UpdatedBy = account.Id;
                    updateOrder.UpdatedAt = currentTime;
                    _unitOfWork.GetRepository<Order>().UpdateAsync(updateOrder);
                    isSuccessful = await _unitOfWork.CommitAsync() > 0;
                    if (!isSuccessful) throw new BadHttpRequestException(MessageConstant.Order.UpdateStatusFailedMessage);
                    return new UpdateOrderResponse(EnumUtil.ParseEnum<OrderStatus>(updateOrder.Status),
                        account.FullName, updateOrder.UpdatedAt, MessageConstant.Order.CanceledStatusMessage);
                    
                default:
                    return new UpdateOrderResponse(EnumUtil.ParseEnum<OrderStatus>(updateOrder.Status),
                        account.FullName, updateOrder.UpdatedAt, MessageConstant.Order.NewStatusMessage);
                    

            }
        }
    }
}
