using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace LeaseBlobDemo
{
    class Program
    {
        public static void Main()
        {
            RunDemo().GetAwaiter().GetResult();
        }

        private static async Task RunDemo()
        {

            string _connectionString = Environment.GetEnvironmentVariable("connection_string");

            //Create instance of the client.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionString);
            CloudBlobClient blobclient = storageAccount.CreateCloudBlobClient();

            //Create a blob container
            CloudBlobContainer cloudBlobContainer = blobclient.GetContainerReference("democontainer");
            await cloudBlobContainer.CreateIfNotExistsAsync();

            //set container to public
            await SetContainerPublic(cloudBlobContainer);

            //upload a file
            await UploadFile(cloudBlobContainer, "File.txt", "Hello World!");

            //acquire a lease of a blob.
            string leaseId = Guid.NewGuid().ToString();
            await LeaseBlob(cloudBlobContainer, "File.txt", leaseId);
            Console.WriteLine($"lease id: {leaseId}");

            //release the lease
            await ReleaseLease(cloudBlobContainer, "File.txt", leaseId);

            //re upload a file with the same name to update File.txt
            await UploadFile(cloudBlobContainer, "File.txt", "Aloha World!");
        }

        private static async Task UploadFile(CloudBlobContainer cloudBlobContainer, string fileName, string fileContent)
        {
            File.WriteAllText(fileName, fileContent);
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
            await cloudBlockBlob.UploadFromFileAsync(fileName);
        }

        private static async Task LeaseBlob(CloudBlobContainer cloudBlobContainer, string blobName, string leaseId)
        {
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
            await cloudBlockBlob.AcquireLeaseAsync(TimeSpan.FromSeconds(30), leaseId);
        }

        private static async Task ReleaseLease(CloudBlobContainer cloudBlobContainer, string blobName, string leaseId)
        {
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);

            await cloudBlockBlob.ReleaseLeaseAsync(new AccessCondition() { LeaseId = leaseId });
        }

        private static async Task SetContainerPublic(CloudBlobContainer cloudBlobContainer)
        {
            var permission = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            };

            await cloudBlobContainer.SetPermissionsAsync(permission);
        }
        private static async Task ListBlobs(CloudBlobContainer cloudBlobContainer)
        {
            Console.WriteLine("Listing blobs from container");
            BlobContinuationToken blobContinuationToken = null;

            do
            {
                var results = await cloudBlobContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);
                blobContinuationToken = results.ContinuationToken;

                foreach (var item in results.Results)
                {
                    Console.WriteLine(item.Uri);
                }
            } while (blobContinuationToken != null);

        }
    }
}
