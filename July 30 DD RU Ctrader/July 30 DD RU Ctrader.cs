// Run Up and Draw Downs save to csv by paolo panicali july 2022

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;


namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FileSystem)]
    public class July30DDRUCtrader : Robot
    {
        System.Random random = new System.Random();

        IndicatorDataSeries MaxDD_Eta;
        IndicatorDataSeries BarsInTradeSerie;
        IndicatorDataSeries MaxDD_Value;

        IndicatorDataSeries MaxRU_Eta;
        IndicatorDataSeries LossBarsInTradeSerie;
        IndicatorDataSeries MaxRU_Value;

        int DD_index = 0, RU_index = 0, counter = 0;
        double SL = 25, TP = 35;

        string filename;
        StreamWriter _fileWriter;

        protected override void OnStart()
        {
            Positions.Closed += PositionsOnClosed;
            MaxDD_Eta = CreateDataSeries();
            MaxDD_Value = CreateDataSeries();
            BarsInTradeSerie = CreateDataSeries();
            MaxRU_Eta = CreateDataSeries();
            LossBarsInTradeSerie = CreateDataSeries();
            MaxRU_Value = CreateDataSeries();
        }

        protected override void OnBar()
        {
            // prepare a csv file to store the results
            if (counter == 0)
            {
                filename = Symbol.Name + "_Trades" + ".csv";
                var filePath = Path.Combine("C:\\Users\\mercu\\OneDrive\\Documenti", filename);
                _fileWriter = File.AppendText(filePath);
                _fileWriter.AutoFlush = true;
                _fileWriter.WriteLine("DateTime" + ";" + "EntryPrice" + ";" + "Close" + ";" + "RunUp" + ";" + "DrawDown" + ";" + "index" + ";" + "OutCome");
                counter = 1;
            }


            // BUY random hour from 1 to 20
            int HourRnd = random.Next(1, 20);
            if (HourRnd == Bars.OpenTimes.LastValue.Hour && Positions.Count() == 0)
            {
                ExecuteMarketOrder(TradeType.Buy, "EURUSD", 1000, "BUY RANDOM: " + Symbol.Name, SL, TP);
            }
            HourRnd = random.Next(1, 20);
            if (HourRnd == Bars.OpenTimes.LastValue.Hour && Positions.Count() == 1)
            {
                CloseAllTrades();
            }
        }


        protected override void OnStop()
        {
            int TotalProfitTrades = MaxDD_Eta.Count();
            MovingAverage DDETA_avg = Indicators.MovingAverage(MaxDD_Eta, TotalProfitTrades, MovingAverageType.Simple);
            Print("Average Draw Down Bars ETA: " + DDETA_avg.Result[TotalProfitTrades - 1]);


            MovingAverage DDValue_avg = Indicators.MovingAverage(MaxDD_Value, TotalProfitTrades, MovingAverageType.Simple);
            Print("Average Draw Down in Pips : " + DDValue_avg.Result[TotalProfitTrades - 1]);

            MovingAverage BarsInTrade_avg = Indicators.MovingAverage(BarsInTradeSerie, TotalProfitTrades, MovingAverageType.Simple);
            Print("Average Bars in Trade : " + BarsInTrade_avg.Result[TotalProfitTrades - 1]);

            ///
            int TotalLossTrades = MaxRU_Eta.Count();

            MovingAverage RUETA_avg = Indicators.MovingAverage(MaxRU_Eta, TotalLossTrades, MovingAverageType.Simple);
            Print("Average Run Up Bars ETA: " + RUETA_avg.Result[TotalLossTrades - 1]);

            MovingAverage RUValue_avg = Indicators.MovingAverage(MaxRU_Value, TotalLossTrades, MovingAverageType.Simple);
            Print("Average Run Up in Pips : " + RUValue_avg.Result[TotalLossTrades - 1]);

            MovingAverage LossBarsInTrade_avg = Indicators.MovingAverage(LossBarsInTradeSerie, TotalLossTrades, MovingAverageType.Simple);
            Print("Average Bars in Trade : " + LossBarsInTrade_avg.Result[TotalLossTrades - 1]);

            Print("Stop Loss Pips :" + SL + "  and Take profit Pips :" + TP);

            // Close the CSV file with raw data on trades
            _fileWriter.Close();
        }



        private void CloseAllTrades()
        {
            // Close Position after N hours for this trading system
            foreach (var position in Positions)
            {

                ClosePosition(position);


            }
        }



        private void PositionsOnClosed(PositionClosedEventArgs args)
        {
            int EntryTime_position_index;
            // find entry bar index
            EntryTime_position_index = 0;
            DateTime EntryTime_position = args.Position.EntryTime;
            for (int i = 0; i < 1000; i++)
            {
                if (Bars.OpenTimes.Last(i) == EntryTime_position)
                {
                    EntryTime_position_index = i + 1;
                    break;
                }
            }

            // position properties
            double EntryPrice_position = args.Position.EntryPrice;
            double StopLoss_position = args.Position.StopLoss.Value;
            double TakeProfit_position = args.Position.TakeProfit.Value;
            double ClosingPrice_position = Bars.ClosePrices.LastValue;
            //better calculating the closing price from the position profit

            // drawdown data for positions went in profit

            if (args.Position.NetProfit > 0)
            {
                Print("Position Closed In Profit :");
                double Position_Min = Bars.LowPrices.Minimum(EntryTime_position_index);
                //double Position_Max = Bars.HighPrices.Maximum(EntryTime_position_index);

                double Position_DD = Math.Round((EntryPrice_position - Position_Min) / Symbol.PipSize);
                //double Position_RU = Position_Max - EntryPrice_position;
                // Draw Down Index
                int MaxDD_position_index = 0;

                for (int i = 0; i < 1000; i++)
                {
                    if (Bars.LowPrices.Last(i) == Position_Min)
                    {
                        MaxDD_position_index = i;
                        break;
                    }
                }
                int MaxDD_ETA_index = EntryTime_position_index - MaxDD_position_index;
                MaxDD_Eta[DD_index] = MaxDD_ETA_index;
                MaxDD_Value[DD_index] = Position_DD;
                BarsInTradeSerie[DD_index] = EntryTime_position_index;

                Print("Bars in Trade : " + BarsInTradeSerie[DD_index] + " Max DD Bar index : " + MaxDD_Eta[DD_index] + "  DD pips  " + MaxDD_Value[DD_index]);

                if (counter == 1)
                {
                    _fileWriter.WriteLine(args.Position.EntryTime + ";" + args.Position.EntryPrice + ";" + Bars.ClosePrices.LastValue + ";" + " " + ";" + MaxDD_Value[DD_index] + ";" + MaxDD_Eta[DD_index] + ";" + "PROFIT");
                }
                DD_index = DD_index + 1;
            }

            // drawdown data for positions went in profit
            if (args.Position.NetProfit < 0)
            {
                Print("Position Closed In LOSS :");
                double Position_Max = Bars.HighPrices.Maximum(EntryTime_position_index);
                double Position_RU = Math.Round((Position_Max - EntryPrice_position) / Symbol.PipSize);
                // Draw Down Index
                int MaxRU_position_index = 0;

                for (int i = 0; i < 1000; i++)
                {
                    if (Bars.HighPrices.Last(i) == Position_Max)
                    {
                        MaxRU_position_index = i;
                        break;
                    }
                }
                int MaxRU_ETA_index = EntryTime_position_index - MaxRU_position_index;
                MaxRU_Eta[RU_index] = MaxRU_ETA_index;
                MaxRU_Value[RU_index] = Position_RU;
                LossBarsInTradeSerie[RU_index] = EntryTime_position_index;
                if (counter == 1)
                {
                    _fileWriter.WriteLine(args.Position.EntryTime + ";" + args.Position.EntryPrice + ";" + Bars.ClosePrices.LastValue + ";" + MaxRU_Value[RU_index] + ";" + " " + ";" + MaxRU_Eta[RU_index] + ";" + "LOSS");
                }
                Print("Bars in Trade : " + LossBarsInTradeSerie[RU_index] + " Max RU Bar index : " + MaxRU_Eta[RU_index] + "  RU pips  " + MaxRU_Value[RU_index]);
                RU_index = RU_index + 1;

            }
        }
    }
}
