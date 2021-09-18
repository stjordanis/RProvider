(**
// can't yet format YamlFrontmatter (["category: Documentation"; "categoryindex: 1"; "index: 1"], Some { StartLine = 2 StartColumn = 0 EndLine = 5 EndColumn = 8 }) to pynb markdown

*)
#r "nuget: RProvider,{{package-version}}"
(**
# R Provider Tutorial

## Referencing the provider

In order to use the R provider, you need to reference the `RDotNet.dll` library
(which is a .NET connector for R) and the `RProvider.dll` itself. For this tutorial,
we use `open` to reference a number of packages including `stats`, `tseries` and `zoo`:

*)
open RDotNet
open RProvider
open RProvider.graphics
open RProvider.stats
open RProvider.tseries
open RProvider.zoo

open System
open System.Net
(**
If either of the namespaces above are unrecognized, you need to install the package in R
using `install.packages("stats")`.

## Obtaining data

In this tutorial, we use [F# Data](http://fsharp.github.io/FSharp.Data/) to access stock
prices from the Yahoo Finance portal. For more information, see the documentation for the
[CSV type provider](http://fsharp.github.io/FSharp.Data/library/CsvProvider.html).

The following snippet uses the CSV type provider to generate a type `Stocks` that can be
used for parsing CSV data from Yahoo. Then it defines a function `getStockPrices` that returns
array with prices for the specified stock and a specified number of days:

*)
#r "FSharp.Data.dll"
open FSharp.Data

type Stocks = CsvProvider<"http://ichart.finance.yahoo.com/table.csv?s=SPX">
 
/// Returns prices of a given stock for a specified number 
/// of days (starting from the most recent)
let getStockPrices stock count =
  let url = "http://ichart.finance.yahoo.com/table.csv?s="
  [| for r in Stocks.Load(url + stock).Take(count).Rows -> float r.Open |] 
  |> Array.rev

/// Get opening prices for MSFT for the last 255 days
let msftOpens = getStockPrices "MSFT" 255
(**
## Calling R functions

Now, we're ready to call R functions using the type provider. The following snippet takes
`msftOpens`, calculates logarithm of the values using `R.log` and then calculates the 
differences of the resulting vector using `R.diff`:

*)
// Retrieve stock price time series and compute returns
let msft = msftOpens |> R.log |> R.diff
(**
If you want to see the resulting values, you can call `msft.AsVector()` in F# Interactive.
Next, we use the `acf` function to display the atuo-correlation and call `adf_test` to
see if the `msft` returns are stationary/non-unit root:

*)
let a = R.acf(msft)
let adf = R.adf_test(msft) 
(**
After running the first snippet, a window similar to the following should appear (note that
it might not appear as a top-most window).

<div style="text-align:center">
<img src="images/acf.png" />
</div>

Finally, we can obtain data for multiple different indicators and use the `R.pairs` function
to produce a matrix of scatter plots:

*)
// Build a list of tickers and get diff of logs of prices for each one
let tickers = 
  [ "MSFT"; "AAPL"; "X"; "VXX"; "SPX"; "GLD" ]
let data =
  [ for t in tickers -> 
      printfn "got one!"
      t, getStockPrices t 255 |> R.log |> R.diff ]

// Create an R data frame with the data and call 'R.pairs'
let df = R.data_frame(namedParams data)
R.pairs(df)
(**
As a result, you should see a window showing results similar to these:

<div style="text-align:center">
<img src="images/pairs.png" />
</div>


*)

