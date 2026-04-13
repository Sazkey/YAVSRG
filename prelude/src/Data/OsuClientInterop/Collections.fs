namespace Prelude.Data.OsuClientInterop
open System.IO
open System.Text
open Percyqaz.Common
open Prelude.Data.Collections
open Prelude.Data
open Prelude.Data.User
open Prelude.Data.Library

module Collections =

    type CollectionImportResult =
        {
            mutable Collections: int
            mutable NewCollections: int
        }
   
    let private import_osu_collections (osu_root_folder: string, library: Library, progress: ProgressCallback) : CollectionImportResult =

        let result = 
        {
            Collections = 0
            NewCollections = 0
        }

        progress (Generic "Reading osu! database")

        let collections =
            use file = Path.Combine(osu_root_folder, "collection.db") |> File.OpenRead
            Logging.Info "Reading collection database..."
            use reader = new BinaryReader(file, Encoding.UTF8)
            OsuCollectionDatabase.Read(reader)

        Logging.Info "Read collection data, containing info about %i collections" collections.Collections.length

        // MD5 -> Imported Interlude data
        let interlude_db_map =
            seq {
                for entry in library.Charts.Entries do
                    for origin in entry.Origins do
                        match origin with
                        | ChartOrigin.Osu osu -> yield osu.Md5, (entry.Hash, osu.FirstNoteOffset, osu.SourceRate)
                        | _ -> ()
            }
            |> Map.ofSeq


        for i, osu_collection in Seq.indexed collections.Collections do

            if osu_collection.BeatmapHashes.length > 0 then

                let mania_beatmaps = ResizeArray<string>()
                for osu_beatmapHash in osu_collection.BeatmapHashes do

                    match Map.tryFind osu_beatmapHash interlude_db_map with
                    | Some (interlude_hash, original_osu_file_first_note, original_osu_file_rate) ->

                        mania_beatmaps.Add(osu_beatmapHash)

                    | None -> Logging.Error "Beatmap %s not imported into Interlude, skipping" osu_beatmapHash

                // todo: create playlist with according name and mania_beatmaps from lookup.

                result.Collections <- result.Collections + 1
