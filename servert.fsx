#time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.Security.Cryptography
open System.Text

let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
            }
            remote {
                helios.tcp {
                    port = 3000
                    hostname = 10.20.206.5
                    
                }
            }
        }")

let mutable reference = null

type Information = 
    | Info of (string*int)
    | Done of (string)
    | Input of (string)
    | Processed of (string*string)


//to keep track of the workers
let system = ActorSystem.Create("RemoteFSharp", configuration)

let WorkerActor (mailbox:Actor<_>)=
    let rec loop()=actor{
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
                    printfn "Response Sent to Client"
                    boss <! Processed("Done", ans.ToString())
                    //printfn $"{ans}"
            // printfn "in"
            boss <! Done("Completed")

        | _ -> ()

        return! loop()
    }
    loop()


let BossActor (mailbox:Actor<_>) =
    let totalActors = 6 // Divided by two to optimize performance.
    let workerActorsPool = 
            [1 .. totalActors]
            |> List.map(fun id -> spawn system (sprintf "Local_%d" id) WorkerActor)

    let workerenum = [|for lp in workerActorsPool -> lp|]
    let workerSystem = system.ActorOf(Props.Empty.WithRouter(Akka.Routing.RoundRobinGroup(workerenum)))
    let mutable completed = 0

    let rec loop () = actor {   

        let! message = mailbox.Receive()
        match message with 

        | Input(header) -> 
        
            for comb_length in [1 .. totalActors] do       //Task Allocation
                workerSystem <! Info(header, comb_length)

        | Processed("Done", ans) -> 
            reference <! "ServerValue" + "," + ans

                    
        | Done("Completed") -> 

            completed <- completed + 1
        
            if completed = totalActors then
                // mailbox.Context.System.Terminate() |> ignore
                //reference <! "ProcessingDone"
                reference <! "ServerProcessingDone"
                system.Terminate() |> ignore
        | _ -> ()
       
        return! loop()
    }
    loop()



let localDispatcherRef = spawn system "localDisp" BossActor
let commlink = 
    spawn system "server"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! msg = mailbox.Receive()
                printfn "%s" msg 
                localDispatcherRef <! Input(msg)
                reference <- mailbox.Sender()
                return! loop() 
            }
        loop()


system.WhenTerminated.Wait()
