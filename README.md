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

# Database

I plan on adding a database to this application using EF but let me hand in my application first so at least I get the process kick started while I work on this. Overall it has been great working on this and a little jolt my brain needed.

# I might've overcooked it but I wanted to take my best swing at it

