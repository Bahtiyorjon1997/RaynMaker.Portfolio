﻿namespace RaynMaker.Portfolio.UseCases

module HistoricalPrices =
    open RaynMaker.Portfolio
    open RaynMaker.Portfolio.Entities

    type private Msg = 
        | Get of Isin * AsyncReplyChannel<Price list>
        | Stop 

    type Api = {
        Get: Isin -> Price list
        Stop: unit -> unit
    }

    let create read =
        let agent = Agent<Msg>.Start(fun inbox ->
            let rec loop store =
                async {
                    let! msg = inbox.Receive()

                    match msg with
                    | Get(isin, replyChannel) -> 
                        let newStore, prices = 
                            match store |> Map.tryFind isin with
                            | Some prices -> store, prices
                            | None -> let prices = read isin
                                      (store |> Map.add isin prices), prices

                        replyChannel.Reply prices

                        return! loop newStore
                    | Stop -> return ()
                }
            loop Map.empty ) 

        agent.Error.Add(handleLastChanceException)
        
        { Get = fun isin -> agent.PostAndReply( fun replyChannel -> (isin,replyChannel) |> Get)
          Stop = fun () -> agent.Post Stop }

module BenchmarkInteractor =
    open System
    open RaynMaker.Portfolio
    open RaynMaker.Portfolio.Entities
    open PositionsInteractor

    type SavingsPlan = {
        Fee : decimal<Percentage>
        AnualFee : decimal<Percentage>
        Rate : decimal<Currency> }
    
    type Benchmark = {
        Isin : Isin
        Name : string }

    let private pricePosition benchmark getPrice day =
        { StockPriced.Date = day
          Name = benchmark.Name
          Isin = benchmark.Isin
          Price = day |> getPrice } 

    /// Based on the original buy & sell events new benchmarking events are generated which simulate 
    /// the performance one could have achived by buying the benchmark asset (e.g. an ETF) instead
    let buyBenchmarkInstead broker (benchmark:Benchmark) getPrice (store:DomainEvent list) =
        let buy day (value:decimal<Currency>) =
            let price = day |> getPrice
            let fee = value |> Broker.getFee broker
            let count = (value - fee) / price
            { StockBought.Isin = benchmark.Isin
              Name = benchmark.Name
              Date = day
              Fee = fee
              Price = price
              Count = count }

        let sell day (value:decimal<Currency>) =
            let price = day |> getPrice
            let fee = value |> Broker.getFee broker
            let count = value / price
            { StockSold.Isin = benchmark.Isin
              Name = benchmark.Name
              Date = day
              Fee = fee
              Price = price
              Count = count }
        
        let lastDay = store |> List.last |> Events.GetDate
        seq {
            yield! store
                    |> Seq.choose (function
                        | StockBought e -> buy e.Date (e.Price * e.Count + e.Fee) |> StockBought |> Some
                        | StockSold e -> sell e.Date (e.Price * e.Count) |> StockSold |> Some
                        | _ -> None)
            yield lastDay |> pricePosition benchmark getPrice |> StockPriced
        }
        |> List.ofSeq

    /// Simulate buying a benchmark whenever a deposit was made considering the cash limit
    let buyBenchmarkByPlan savingsPlan (benchmark:Benchmark) getPrice (start:DateTime) (stop:DateTime) =
        let buy day  =
            let price = day |> getPrice
            let value = savingsPlan.Rate
            let fee = value |> percent savingsPlan.Fee
            let count = (value - fee) / price
            { StockBought.Isin = benchmark.Isin
              Name = benchmark.Name
              Date = day
              Fee = fee
              Price = price
              Count = count }

        // TODO: anual fee
        // -> what about just inventing an event because it could then be calculated when walking the positions
    
        let numMonths = (stop.Year - start.Year) * 12 + (stop.Month - start.Month)
        
        seq {
            yield!  [0 .. numMonths]
                    |> Seq.map(fun m -> (new DateTime(start.Year, start.Month, 1)).AddMonths(m))
                    |> Seq.map(fun day -> day |> buy |> StockBought)
            yield stop |> pricePosition benchmark getPrice |> StockPriced
        }
        |> List.ofSeq

    type Performance = {
        BuyInstead : PositionEvaluation
        BuyPlan : PositionEvaluation }
    
    let getBenchmarkPerformance (store:EventStore.Api) broker savingsPlan (historicalPrices:HistoricalPrices.Api) (benchmark:Benchmark) = 
        let getPrice day = 
            let history = benchmark.Isin |> historicalPrices.Get
            match Prices.getPrice 30.0 history day with
            | Some p -> p
            | None -> failwithf "Could not get a price for: %A" day

        let eval events = 
            events
            |> Positions.create
            |> PositionsInteractor.evaluatePositions broker (Events.LastPriceOf events)
            |> Seq.head

        let events = store.Get()

        {
            BuyInstead = events |> buyBenchmarkInstead broker benchmark getPrice |> eval
            BuyPlan = 
                let start = events.Head |> Events.GetDate
                let stop = events |> List.last |> Events.GetDate
                buyBenchmarkByPlan savingsPlan benchmark getPrice start stop |> eval
        }