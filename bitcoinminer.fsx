#time "on"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
open System
open Akka.Actor
open Akka.FSharp
open System.IO
open System.Security.Cryptography
open System.Text

let system = ActorSystem.Create("Squares", Configuration.defaultConfig())
type Information = 
    | Info of (string*int)
    | Done of (string)
    | Input of (string)

// Printer Actor - To print the output
let printerActor (mailbox:Actor<_>) = 
    let rec loop () = actor {
        let! (index:int64) = mailbox.Receive()
        printfn "%d" index      
        return! loop()
    }
    loop()
let printerRef = spawn system "Printer" printerActor

// Worker Actors - Takes input from Boss and do the processing using sliding window algo and returns the completed message.
let WorkerActor (mailbox:Actor<_>) =
    let rec loop () = actor {
        let! message = mailbox.Receive()
        let boss = mailbox.Sender()
        match message with

        | Info(h, comb_length) -> 

            let rec combinations acc size set = seq {
              match size, set with 
              | n, x::xs -> 
                  if n > 0 then yield! combinations (x::acc) (n - 1) xs
                  if n >= 0 then yield! combinations acc n xs 
              | 0, [] -> yield acc 
              | _, [] -> () }

            
            let mutable answer = 0
            let result = combinations [] comb_length ['a'; 'b'; 'c'; 'd'; 'e'; 'f' ; 'g'; 'h' ; 'i'; 'j'; 'k'; 'l'; 'm'; 'n'; 'o'; 'p'; 'q'; 'r'; 's'; 't'; 'u'; 'v'; 'w'; 'x'; 'y'; 'z'; '1';'2'; '3'; '4'; '5'; '6'; '7'; '8'; '9'; '0']
                
            for x in result do

                let string = "nik.gupta" + System.String.Concat(Array.ofList(x))
                let mySHA256 = SHA256Managed.Create()
                let enc = Encoding.UTF8;
                let temp = enc.GetBytes(string)
                let res = mySHA256.ComputeHash(temp)
                let mutable Sb = ""

                for x in res do 
                    Sb <- Sb + x.ToString("x2")  
                
                let length = String.length h 
                
                let first_k = Sb.[0 .. length-1] 
                
                if first_k = h && Sb.[length] <> '0' then 
                    let ans = string + "\t" + Sb 
                    printfn $"{ans}"
                    answer <- answer + 1 

            boss <! Done("Completed")

        | _ -> ()

        return! loop()
    }
    loop()
             
// Boss - Takes input from command line and spawns the actor pool. Splits the tasks based on cores count and allocates using Round-Robin
let BossActor (mailbox:Actor<_>) =
    
    let totalactors = 4
    
    let workerActorsPool = 
            [1 .. totalactors]
            |> List.map(fun id -> spawn system (sprintf "Local_%d" id) WorkerActor)

    let workerenum = [|for lp in workerActorsPool -> lp|]
    let workerSystem = system.ActorOf(Props.Empty.WithRouter(Akka.Routing.RoundRobinGroup(workerenum)))
    
    let mutable completed = 0

    let rec loop () = actor {

        let! message = mailbox.Receive()
        match message with 

        | Input(header) -> 
            
            for comb_length in [1 .. totalactors] do
                workerSystem <! Info(header, comb_length)
                    
        | Done("Completed") ->

            completed <- completed + 1
        
            if completed = totalactors then
                mailbox.Context.System.Terminate() |> ignore
        | _ -> ()
       
        return! loop()
    }
    loop()

let boss = spawn system "boss" BossActor
// Input from Command Line
let K = 3
let mutable header = ""

for i in 1 .. K do 
    header <- header + "0"

printfn "Bitcoin mining started!"
boss <! Input(header)
system.WhenTerminated.Wait()
printfn "Bitcoin mining ended!"