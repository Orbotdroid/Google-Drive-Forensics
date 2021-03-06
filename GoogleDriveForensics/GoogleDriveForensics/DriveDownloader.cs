﻿using Google.Apis.Drive.v2.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDriveForensics
{
    public class DriveDownloader
    {
        //Folder to store downloaded files
        private string FOLDER_PATH;

        private DriveScanner driveScanner;

        //Empty default constructor
        public DriveDownloader(DriveScanner scanner)
        {
            driveScanner = scanner;
            FOLDER_PATH = scanner.FOLDER_PATH;
        }


        //Write important metadata of all files to a txt file.
        public async Task DownloadSummaryAsync()
        {
            using (var stream = System.IO.File.Create(Path.Combine(FOLDER_PATH, "result.txt"))) { }

            await driveScanner.BlockingProcessAsync(file => WriteFileToSummary(file));
        }
        //List important metadata of a single file
        private void WriteFileToSummary(Google.Apis.Drive.v2.Data.File fileEntry)
        {
            using (StreamWriter writer = System.IO.File.AppendText(Path.Combine(FOLDER_PATH, "result.txt")))
            {
                writer.WriteLine("File ID: " + fileEntry.Id);
                writer.WriteLine("Title: " + fileEntry.Title);
                writer.WriteLine("Original Filename: " + fileEntry.OriginalFilename);
                writer.WriteLine("Md5Checksum: " + fileEntry.Md5Checksum);
                writer.WriteLine("File Size: " + fileEntry.FileSize);
                writer.WriteLine("MIME type: " + fileEntry.MimeType);
                writer.WriteLine("Created Date: " + fileEntry.CreatedDate);
                writer.WriteLine("Modified Date: " + fileEntry.ModifiedDate);
                writer.WriteLine("Last Modifying User: " + fileEntry.LastModifyingUser.DisplayName);
                writer.WriteLine("Last Viewed By Me Date: " + fileEntry.LastViewedByMeDate);

                writer.Write(writer.NewLine);
                writer.WriteLine("Shared: " + fileEntry.Shared);
                if (fileEntry.SharingUser != null)
                    writer.WriteLine("Sharing User: " + fileEntry.SharingUser.DisplayName);
                writer.WriteLine("Last modified by me: " + fileEntry.ModifiedByMeDate);
                writer.WriteLine("Download URL: " + fileEntry.DownloadUrl);

                writer.WriteLine("Explicitly Trashed: " + fileEntry.ExplicitlyTrashed);

                writer.Write(writer.NewLine);
                writer.WriteLine("======================================");
                writer.Write(writer.NewLine);

                Console.WriteLine(fileEntry.Title + " recorded.");
            }
        }



        //Download metadata of a file via file entry ID
        public async Task DownloadMetadataAsync(string fileId)
        {
            Google.Apis.Drive.v2.Data.File file = await driveScanner.getFileEntryAsync(fileId);

            await DownloadMetadataAsync(file);
        }
        //Download metadata of a file via file entry
        public async Task DownloadMetadataAsync(Google.Apis.Drive.v2.Data.File fileEntry)
        {
            try
            {
                //Use HTTP client in DriveService to obtain response
                Task<Stream> fileJsonStreamTask = driveScanner.GetMetaDataStreamAsync(fileEntry);

                using (Stream jsonStream = await fileJsonStreamTask)
                {
                    if (jsonStream != null)
                        await WriteStreamToFile(jsonStream,
                            Path.Combine(FOLDER_PATH, "Metadata"), fileEntry.Title + ".json");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while writing JSON file: " + e.Message);
            }
        }
        //Download all JSON files as metadata record
        public async Task DownloadAllMetadataAsync()
        {
            await driveScanner.ParrallelProcessAsync(file => DownloadMetadataAsync(file));
        }


        //Download content of a file via file entry ID
        public async Task DownloadContentAsync(string fileId)
        {
            Google.Apis.Drive.v2.Data.File file = await driveScanner.getFileEntryAsync(fileId);

            await DownloadContentAsync(file);
        }
        //Download content of a file via file entry
        public async Task DownloadContentAsync(Google.Apis.Drive.v2.Data.File fileEntry)
        {
            try
            {
                //Use HTTP client in DriveService to obtain response
                Task<Stream> fileContentStreamTask = driveScanner.GetContentStreamAsync(fileEntry);

                using (Stream contentStream = await fileContentStreamTask)
                {
                    if (contentStream != null)
                        await WriteStreamToFile(contentStream,
                            Path.Combine(FOLDER_PATH, "Content"), fileEntry.Title);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while writing file: " + e.Message);
            }
        }
        //Download all file contents
        public async Task DownloadAllContentsAsync()
        {
            await driveScanner.ParrallelProcessAsync(file => DownloadContentAsync(file));
        }



        //Download revisions of a particular file
        public async Task downloadAllRevisions(string fileId)
        {
            Google.Apis.Drive.v2.Data.File file = await driveScanner.getFileEntryAsync(fileId);

            await downloadAllRevisions(file);
        }
        //Download revisions of a particular file
        public async Task downloadAllRevisions(Google.Apis.Drive.v2.Data.File fileEntry)
        {
            if (fileEntry.MimeType == "application/vnd.google-apps.folder")
                return;

            string fileID = fileEntry.Id;
            IList<Revision> revisions = await driveScanner.getRevisionsAsync(fileID);

            foreach(var version in revisions)
            {
                string revID = version.Id;

                try
                {
                    Task<Stream> fileRevisionStream = driveScanner.GetRevisionStreamAsync(fileID, revID);

                    using(Stream revisionStream = await fileRevisionStream)
                    {
                        if(revisionStream!=null)
                        {
                            await WriteStreamToFile(revisionStream, 
                                Path.Combine(FOLDER_PATH, "Revisions"), fileEntry.Title + " - " + revID + ".json");
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error while downloading revisions: " + e.Message);
                }
            }
        }


        private async Task WriteStreamToFile(Stream stream, string folderPath, string filename)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            //Write response stream to file
            using (FileStream output = System.IO.File.Open(Path.Combine(folderPath, filename), FileMode.Create))
            {
                Task writeFile = stream.CopyToAsync(output);
                Console.WriteLine("-----Writing {0}...", filename);
                await writeFile;
                Console.WriteLine("-----" + filename + " has been written to disk.");
            }
            Console.WriteLine();
        }
    }
}
