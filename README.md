# Solution Summary

## Link to Main Project

https://github.com/Taxually-Engineering/ai-assisted-coding-challenge

## ---- ##
Firstly most of the code was written by me. Claude Code was a great help with understanding some parts of the code and the logic of how the forex works. It also gave me a comprehensive guide on how to refactor code which I used as a guideline to how I approached this exercise

## ---- ##

What I first did after ensuring the tests work was to try and break the program into smaller chunks this action allowed me to see what parts of the code are functional and necessary and which are not (I ended up moving it back into one file though because of the data store / global variables and it was cumbersome to be passing objects in and out)

## ---- ##

I noticed that functions like EnsureMinimumDateRange was repeated multiple times albeit with different parameters so I simplified it into one function that is clear and readable. I also went round most of the other functions and either simplified it or outright got rid of things that were not needed / functional to the program (Can always implement them later in a better way)

## ---- ##

The next issue I encountered was the fact that the providers was being injected as dependencies and this is a problem for two reasons 

1. If I want to add a new provider or remove a provider. I have to go into the program.cs and remove the dependency and also remove the dedicated classes for them and pray to God the errors aren't mind bending.
2. All of the providers have basically the same functions and it was a lot of duplicated code and multiple files that do the same thing

So I removed this and added an enumerable to store a list of providers their currency, update frequency and the likes now supporting different providers is as easy as editing one file and it works everywhere.

## ---- ##

For the providers the functions to get their rates either weekly or daily or anyhow is added into a combined class and the class has distinct functions for each action. This is practically the only class needed to access the external API's. 

## ---- ##

I also removed the second get rate function and merged it into the main one with more parameters because there was no need for another smaller function taking up space and complexity when it is about 3 lines of code to add it to the main GetRate() function

## ---- ##

If a different rate has been returned for a specific time then instead of throwing an error the object is updated in the dictionary. in the AddRateToDictionaries function. I also had to make changes to the functions at the end of the tests like the invalid currency code one because the response it was expecting was an error 500 but the system returns a better error 404

## -- NEW ADDITION -- ##

Updated the codebase to include the additional files

Added a new file called ForexModels and this holds the model to be used instead of the nested dictionary "Dictionary<(ExchangeRateSources, ExchangeRateFrequencies), Dictionary<CurrencyTypes, Dictionary<DateTime, decimal>>> _fxRatesBySourceFrequencyAndCurrency;

and

Dictionary<(ExchangeRateSources, ExchangeRateFrequencies), DateTime> _minFxDateBySourceAndFrequency;" 

This makes the code vastly more readable as objects are easy to follow. Debugging is easy as you can look at the parameters of the object and print it as a json easily and more importantly it can be passed around to different classes in different files

Also updated the memory store in the program.cs file to update the rate if it exists in the database.

Made changes to the EnsureMinimumDateRange function so if a rate is not found and there is no corresponding rate for the item then the default date passed is the end of the month not the current date. This will ensure that the External API only fetches the relevant month data not the half a year data it currently fetches.

## -- NEW ADDITION -- ##

Moved the class that handles the memory store out of the program.cs file and into a new class called DatabaseStore. This can be edited or renamed later depending on the use cases

## -- NEW ADDITION -- ##

Made changes to the API endpoint in the program.cs file so if any parameter is not provided or is not in the correct format then the API will throw an error message and also link them to the documentation so they can get the proper format and test again

## -- NEW ADDITION -- ##

Changed the function in CombinedExternalApiExchangeRateProvider.cs that were calling async functions e.g getdailyexchangerateasync to now be async so there is no deadlock or any unexpected behaviours like missed errors and the like.

To aid with this I created a mini helper class, Get_Enumerable_From_IEnumerable that I then called throughout the main ExchangeRateRepository class 

## -- NEW ADDITION -- ##

To handle the case where an exchange rate is too old I put a check where if the minFxDate is older than 30 days from the date requested then it isn't used for the forex calculation. This is a good balance of failure proofing and also some leeway for the clients using the system. The variable is Exchange_Rate_Leeway

## -- NEW ADDITION -- ##

I removed the registered providers and added them into a specific json file RegisteredProviders.json. this file can be edited at any time and added to the directory of the program. I also registered the service with dependency injection to make it accessible to any class that needs it. This way the program is more modular than ever.

## -- NEW ADDITION -- ##

I moved the calculation out from GetFxRate into a Calculate_FX_Rates, this returns a decimal and this means we can now test different rates with different quote types to get the rates. (More work is needed to remove the cross rates calculation into this function or class)

# Database (No Longer Needed)

I plan on adding a database to this application using EF but let me hand in my application first so at least I get the process kick started while I work on this. Overall it has been great working on this and a little jolt my brain needed.

# I might've overcooked it but I wanted to take my best swing at it

