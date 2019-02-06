# Durable Functions Analyzer

This is a collection of analyzers to save you from making some common mistakes with [Durable Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview). 

![An example of one of the analyzers finding an incorrectly named function call](images/poc.png)

## The why

Function calls in durable functions are written in a way which can introduce errors in your code which won't be found until run time. In the interests of shifting warnings left these analyzers catch a number of common mistakes.

* Using the wrong name to refer to a function call
* <span style="color: grey">Passing the wrong arguments</span> (not just yet)
* <span style="color: grey">Casting to the wrong return type </span> (not just yet)

## How to use them