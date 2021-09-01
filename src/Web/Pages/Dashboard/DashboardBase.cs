﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using APIClient.Api;
using APIClient.Model;
using ChartJs.Blazor;
using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.Common.Enums;
using ChartJs.Blazor.LineChart;
using ChartJs.Blazor.PieChart;
using ChartJs.Blazor.Util;
using Microsoft.AspNetCore.Components;
using Web.Shared;

namespace Web.Pages.Dashboard
{
    public class DashboardBase : ComponentBase
    {
        private Random random = new();
        protected readonly PieConfig _orderStatusPieConfig = new();
        protected readonly PieConfig _pricePerProductCostPieChartConfig = new();
        protected readonly BarConfig _pricePerWorkerBarChartConfig = new();
        protected readonly LineConfig _costPerProductLineChartConfig = new();
        protected readonly LineConfig _costPerWorkerLineChartConfig = new();

        protected Chart _orderStatusPieChart;
        protected Chart _pricePerProductCostPieChart;
        protected Chart _pricePerWorkerBarChart;
        protected Chart _costPerProductLineChart;
        protected Chart _costPerWorkerLineChart;

        [Inject]
        public IHttpClientFactory HttpClientFactory { get; set; }

        protected List<OrderListResult> OrderList { get; set; } = new();

        protected List<OrderListResult> OngoingOrders { get; set; } = new();

        protected List<OrderListResult> UpcomingOrders { get; set; } = new();

        protected List<ExpenseListResult> ExpenseList { get; set; } = new();

        protected List<WorkerListResult> WorkerList { get; set; } = new();

        protected override void OnInitialized()
        {
            RenderCharts();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                OrderList = await new OrdersApi().OrderListAsync();
                ExpenseList = await new ExpensesApi().ExpenseListAsync();
                WorkerList = await new WorkersApi().WorkerListAsync();

                OngoingOrders = OrderList.Where(x => x.OrderStatus == OrderStatus.OnGoing).OrderByDescending(x => x.StartDate).ToList();
                UpcomingOrders = OrderList.Where(x => x.OrderStatus == OrderStatus.NotStarted && x.StartDate.Value > DateTime.UtcNow.AddDays(1)).OrderByDescending(x => x.StartDate).ToList();

                PopulateOrderStatusPieChart();
                PopulateBarChartForPricePerWorkerData();
                PopulatePieChartForPricePerProductData();
                PopulateLineChartForProductPerWeekData();
                PopulateLineChartForProductPerWorkerData();

                // reloads states and components, updating with data from the async calls above
                await InvokeAsync(StateHasChanged);
            }
        }

        protected double CalculateTotalPriceForOrder(int orderId) => Math.Round(ExpenseList.Where(x => x.OrderId == orderId).Sum(x => x.Price), 2);

        #region Render charts
        private void RenderCharts()
        {
            RenderPieChartForOrderStatus();
            RenderPieChartForPricePerProductCost();
            RenderBarChartForPricePerWorker();
            RenderLineChartForProductPerWorkerCost();
            RenderLineChartForProductPerWeekCost();
        }

        private void RenderPieChartForOrderStatus()
        {
            _orderStatusPieConfig.Options = new PieOptions
            {
                Responsive = true,
                Title = new OptionsTitle
                {
                    Display = true,
                    Text = "Number of orders per status",
                },
            };

            foreach (var status in Enum.GetValues<OrderStatus>())
            {
                // since we will have a lot of finished in the end, this will clutter the chart.
                // until we have filtering/date picking for range, we do not include it
                if (status == OrderStatus.Finished)
                {
                    continue;
                }

                _orderStatusPieConfig.Data.Labels.Add(status.ToString());
            }
        }

        private void RenderPieChartForPricePerProductCost()
        {
            _pricePerProductCostPieChartConfig.Options = new PieOptions
            {
                Responsive = true,
                Title = new OptionsTitle
                {
                    Display = true,
                    Text = "Cost per product type",
                },
            };

            foreach (var product in Enum.GetValues<Product>())
            {
                _pricePerProductCostPieChartConfig.Data.Labels.Add(product.ToString());
            }
        }

        private void RenderBarChartForPricePerWorker()
        {
            _pricePerWorkerBarChartConfig.Options = new BarOptions
            {
                Responsive = true,
                Legend = new Legend
                {
                    Position = Position.Top,
                },
                Title = new OptionsTitle
                {
                    Display = true,
                    Text = "Price per worker",
                },
            };
        }

        private void RenderLineChartForProductPerWeekCost()
        {
            _costPerProductLineChartConfig.Options = new LineOptions
            {
                Responsive = true,
                Title = new OptionsTitle
                {
                    Display = true,
                    Text = "Cost per product per week",
                },
                Tooltips = new Tooltips
                {
                    Mode = InteractionMode.Nearest,
                    Intersect = true,
                },
                Hover = new Hover
                {
                    Mode = InteractionMode.Nearest,
                    Intersect = true,
                },
                Scales = new Scales
                {
                    XAxes = new List<CartesianAxis>
                        {
                            new CategoryAxis
                            {
                                ScaleLabel = new ScaleLabel
                                {
                                    LabelString = "Week",
                                },
                            },
                        },
                    YAxes = new List<CartesianAxis>
                        {
                            new LinearCartesianAxis
                            {
                                ScaleLabel = new ScaleLabel
                                {
                                    LabelString = "Cost",
                                },
                            },
                        },
                },
            };
        }

        private void RenderLineChartForProductPerWorkerCost()
        {
            _costPerWorkerLineChartConfig.Options = new LineOptions
            {
                Responsive = true,
                Title = new OptionsTitle
                {
                    Display = true,
                    Text = "Cost per worker per week",
                },
                Tooltips = new Tooltips
                {
                    Mode = InteractionMode.Nearest,
                    Intersect = true,
                },
                Hover = new Hover
                {
                    Mode = InteractionMode.Nearest,
                    Intersect = true,
                },
                Scales = new Scales
                {
                    XAxes = new List<CartesianAxis>
                        {
                            new CategoryAxis
                            {
                                ScaleLabel = new ScaleLabel
                                {
                                    LabelString = "Week",
                                },
                            },
                        },
                    YAxes = new List<CartesianAxis>
                        {
                            new LinearCartesianAxis
                            {
                                ScaleLabel = new ScaleLabel
                                {
                                    LabelString = "Cost",
                                },
                            },
                        },
                },
            };
        }

        #endregion

        #region Popuelate charts from asynchrounos data
        private void PopulateOrderStatusPieChart()
        {
            var dataSet = new PieDataset<int>(
                new[]
                {
                    OrderList.Count(x => x.OrderStatus == OrderStatus.NotStarted),
                    OrderList.Count(x => x.OrderStatus == OrderStatus.OnGoing),
                    OrderList.Count(x => x.OrderStatus == OrderStatus.OnHold),
                })
            {
                // move these to configuration/static utils (random numbers does not tell the correct story)
                BackgroundColor = new[]
                {
                    ColorUtil.ColorHexString(255, 99, 132), // Slice 1 aka "Red"
                    ColorUtil.ColorHexString(255, 205, 86), // Slice 2 aka "Yellow"
                    ColorUtil.ColorHexString(75, 192, 192), // Slice 3 aka "Green"
                },
            };

            _orderStatusPieConfig.Data.Datasets.Add(dataSet);
        }

        private void PopulatePieChartForPricePerProductData()
        {
            var dataSet = new PieDataset<double>(
                new[]
                {
                    Convert.ToDouble(random.Next(150, 25000)),
                    Convert.ToDouble(random.Next(150, 25000)),
                    Convert.ToDouble(random.Next(150, 25000)),
                    Convert.ToDouble(random.Next(150, 25000)),
                })
            {
                BackgroundColor = new[]
                {
                    ColorUtil.ColorHexString(255, 99, 132), // Slice 1 aka "Red"
                    ColorUtil.ColorHexString(255, 205, 86), // Slice 2 aka "Yellow"
                    ColorUtil.ColorHexString(75, 192, 192), // Slice 3 aka "Green"
                    ColorUtil.ColorHexString(54, 162, 235), // Slice 4 aka "Blue"
                },
            };

            _pricePerProductCostPieChartConfig.Data.Datasets.Add(dataSet);
        }

        private void PopulateBarChartForPricePerWorkerData()
        {
            var workerIdListOrderedByName = WorkerList.OrderBy(x => x.Name).Select(x => new {x.WorkerId, x.Name}).Take(8);

            foreach (var worker in workerIdListOrderedByName)
            {
                _pricePerWorkerBarChartConfig.Data.Labels.Add(worker.Name);
            }

            var expenseList = workerIdListOrderedByName.Select(x => ExpenseList.Where(w => w.WorkerId == x.WorkerId).Sum(s => s.Price));

            var dataSet = new BarDataset<double>(expenseList)
            {
                Label = "Expense per worker",
                BackgroundColor = ColorUtil.FromDrawingColor(Color.FromArgb(128, Utils.ChartColors.Red)),
                BorderColor = ColorUtil.FromDrawingColor(Utils.ChartColors.Red),
                BorderWidth = 1,
            };

            _pricePerWorkerBarChartConfig.Data.Datasets.Add(dataSet);
        }

        private void PopulateLineChartForProductPerWeekData()
        {
            _costPerProductLineChartConfig.Data.Labels.AddRange(Enumerable.Range(0, 7).Select(x => $"Week {x+1}"));

            foreach (var product in Enum.GetValues<Product>())
            {
                IDataset<int> data = new LineDataset<int>(Enumerable.Range(0, 7).Select(x => x * random.Next(1500, 234234)))
                {
                    Label = product.ToString(),
                    BackgroundColor = ColorUtil.FromDrawingColor(Utils.ChartColors.All[(int)product]),
                    BorderColor = ColorUtil.FromDrawingColor(Utils.ChartColors.All[(int)product]),
                    Fill = FillingMode.Disabled,
                };

                _costPerProductLineChartConfig.Data.Datasets.Add(data);
            }
        }

        private void PopulateLineChartForProductPerWorkerData()
        {
            _costPerWorkerLineChartConfig.Data.Labels.AddRange(Enumerable.Range(0, 7).Select(x => $"Week {x+1}"));

            int index = 0;
            foreach (var product in new [] { "Worker A", "Worker B", "Worker C", "Worker D"})
            {
                IDataset<int> data = new LineDataset<int>(Enumerable.Range(0, 7).Select(x => x * random.Next(1500, 234234)))
                {
                    Label = product,
                    BackgroundColor = ColorUtil.FromDrawingColor(Utils.ChartColors.All[index]),
                    BorderColor = ColorUtil.FromDrawingColor(Utils.ChartColors.All[index]),
                    Fill = FillingMode.Disabled,
                };

                _costPerWorkerLineChartConfig.Data.Datasets.Add(data);

                index++;
            }
        }

        #endregion
    }
}
