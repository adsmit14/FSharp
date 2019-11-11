open System
open System.IO
open Argu

type CLIArguments =
    | [<Mandatory>] Path of path:string
    | [<Mandatory>] Title of title:string
    | [<Mandatory>] Author of author:string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Path _ -> "specify a path"
            | Title     _ -> "specify a title"
            | Author    _ -> "specify an author"

let (|Int|_|) str =
   match System.Int32.TryParse(str) with
   | (true,int) -> Some(int)
   | _ -> None

[<EntryPoint>]
let main argv =

    let parser = ArgumentParser.Create<CLIArguments>(
                    programName = "AudiobookRenamer.exe", 
                    errorHandler = ProcessExiter())

    let results = parser.ParseCommandLine argv
    let path = results.GetResult(<@ Path @>)
    let author = results.GetResult(<@ Author @>)
    let title = results.GetResult(<@ Title @>)

    let output = Directory.CreateDirectory(Path.Combine(path, "Output"))

    let sourceFiles = 
        Directory.EnumerateFiles(path, "*.m4b", SearchOption.AllDirectories)
        |> Seq.sortBy (fun x -> match Path.GetFileNameWithoutExtension(x) 
                                    with
                                        | Int y -> y
                                        | _ -> 0)
        |> Seq.toArray

    let count = Array.length sourceFiles

    let destFiles = [| 1..count |]
                 |> Array.map (fun x -> sprintf "%02d %s - %s.m4b" x author title)
                 |> Array.map (fun x -> Path.Combine(output.FullName, x))
   
    destFiles
    |> Array.zip sourceFiles 
    |> Array.iter (fun (f1, f2) -> File.Copy(f1, f2))

    destFiles 
    |> Array.map (fun x -> TagLib.File.Create(x))
    |> Array.zip [| 1..count |] 
    |> Array.iter (fun (x, y) -> 
            y.Tag.Title <- sprintf "Part %02d" x
            y.Tag.Album <- title
            y.Tag.AlbumArtists <- [| author |]
            y.Tag.Track <- (uint32) x
            y.Tag.TrackCount <- (uint32) count
            y.Save()
        )
    0
    