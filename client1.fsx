#time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.Security.Cryptography
open System.Text
open Akka.Event


let k = 4
let mutable header= ""
for i in 1 .. k do 
    header <- header + "0"

printfn "Bitcoin mining started!"

let ServerIP = fsi.CommandLineArgs.[1] |> string
let ServerPort = fsi.CommandLineArgs.[2] |>string
//"akka.tcp://RemoteFSharp@localhost:8777/user/server"
let addr = "akka.tcp://RemoteFSharp@" + ServerIP + ":" + ServerPort + "/user/server"


let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                
            }
            remote {
                helios.tcp {
                    port = 8778
                    hostname = localhost
                }
            }
        }")

let system = ActorSystem.Create("ClientFsharp", configuration)


type Information = 
    | Info of (string*int)
    | Done of (string)
    | Input of (string)


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
                    printfn "From this Client"
                    printfn $"{ans}"
                    answer <- answer + 1 

            boss <! Done("Completed")

        | _ -> ()

        return! loop()
    }
    loop()
let mutable Local_Work = false

             
// Boss - Takes input from command line and spawns the actor pool. Splits the tasks based on cores count and allocates using Round-Robin
let BossActor (mailbox:Actor<_>) =
    
    let totalactors = 5
    
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
                Local_Work <- true
        | _ -> ()
       
        return! loop()
    }
    loop()

let mutable Remote_Work = false
let commlink = 
    spawn system "client"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! msg = mailbox.Receive()
                printfn "%s" msg 
                let response =msg|>string
                let command = (response).Split ','
                if command.[0].CompareTo("init")=0 then
                    let echoClient = system.ActorSelection(addr)
                    let msgToServer = (header)
                    echoClient <! msgToServer
                    let dispatcherRef = spawn system "Dispatcher" BossActor
                    dispatcherRef <! Input(header)
                elif command.[0].CompareTo("ServerValue")=0 then
                    printfn "Received from Server"
                    printfn "%s" command.[1]
                elif response.CompareTo("ServerProcessingDone")=0 then
                    system.Terminate() |> ignore
                    Remote_Work <- true
                else
                    printfn "-%s-" msg

                return! loop() 
            }
        loop()

printfn "Bitcoin mining started!"
commlink <! "init"
while (not Local_Work && not Remote_Work) do
system.WhenTerminated.Wait()
printfn "Bitcoin mining ended!"