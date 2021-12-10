# BitcoinMiner
Bitcoin miner using AKKA actor library in F# with remote server/client
Background:
In this project, we have to use exclusively the AKKA actor library in F#.  Worker actors are given a range of problems to solve and a boss that keeps track of all the problems and performs the job assignment.
The key component in a bit-coin is an input that, when “hashed” produces an output smaller than a target value.  In practice, the comparison values have leading  0’s, thus the bitcoin is required to have a given number of leading 0’s
Here, we are required to use SHA-26 to find hashes for an input string

Steps to run:</br>
Requirements: .NET SDK, Ionide, Visual Studio</br>

On Local Machine: </br>
(Tested on Windows and Mac OS) </br>
Go to the project directory and and then into the Local subdirectory</br>
Type in the visual studio terminal (or Command Prompt): dotnet fsi bitcoinminer.fsx 4</br>
The argument is the value of 'k'- which is the number of leading 0’s in the hashed output</br>
The program will terminate when all bitcoins with k leading 0’s are mined</br>
General command: dotnet fsi .\proj1.fsx </br>

On Remote Server:
(Tested on Windows and Mac OS)</br>
Go to the project directory and then into the Remote subdirectory</br>
Open two terminal windows one for server.fsx and other with client.fsx</br>
Run server.fsx first and wait until the server is listening for requests</br>
Run client.fsx next with 2 arguments:</br>
 - Ip address of the machine (as mentioned in server.fsx configuration)
 - Port No (as mentioned in server.fsx configuration)</br>

Both client and server will terminate when all bitcoins with k leading 0’s are mined
