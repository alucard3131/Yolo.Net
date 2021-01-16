﻿using Alturos.Yolo.Model;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Alturos.Yolo
{
    public class YoloPreTrainedDataSetRepository
    {
        private readonly YoloPreTrainedData[] _preTrainedData;

        public YoloPreTrainedDataSetRepository()
        {
            _preTrainedData = new[]
            {
                new YoloPreTrainedData
                {
                    Name = "YOLOv3",
                    ConfigFileUrl = "https://raw.githubusercontent.com/AlexeyAB/darknet/master/cfg/yolov3.cfg",
                    NamesFileUrl = "https://raw.githubusercontent.com/AlexeyAB/darknet/master/cfg/coco.names",
                    WeightsFileUrl = "https://pjreddie.com/media/files/yolov3.weights"
                },
                new YoloPreTrainedData
                {
                    Name = "YOLOv3-tiny",
                    ConfigFileUrl = "https://raw.githubusercontent.com/AlexeyAB/darknet/master/cfg/yolov3-tiny.cfg",
                    NamesFileUrl = "https://raw.githubusercontent.com/AlexeyAB/darknet/master/cfg/coco.names",
                    WeightsFileUrl = "https://pjreddie.com/media/files/yolov3-tiny.weights"
                },
                new YoloPreTrainedData
                {
                    Name = "YOLOv2",
                    ConfigFileUrl = "https://raw.githubusercontent.com/AlexeyAB/darknet/master/cfg/yolov2.cfg",
                    NamesFileUrl = "https://raw.githubusercontent.com/AlexeyAB/darknet/master/cfg/coco.names",
                    WeightsFileUrl = "https://pjreddie.com/media/files/yolov2.weights"
                },
                new YoloPreTrainedData
                {
                    Name = "YOLOv2-tiny",
                    ConfigFileUrl = "https://raw.githubusercontent.com/AlexeyAB/darknet/master/cfg/yolov2-tiny.cfg",
                    NamesFileUrl = "https://raw.githubusercontent.com/pjreddie/darknet/master/data/voc.names",
                    WeightsFileUrl = "https://pjreddie.com/media/files/yolov2-tiny.weights"
                }
            };
        }

        public async Task<string[]> GetDataSetsAsync()
        {
            var names = _preTrainedData.Select(o => o.Name).ToArray();
            return await Task.FromResult(names);
        }

        public async Task<bool> DownloadDataSetAsync(string name, string destinationPath)
        {
            Directory.CreateDirectory(destinationPath);

            var preTrainedData = _preTrainedData.Where(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (preTrainedData == null)
            {
                return false;
            }

            if (!await DownloadAsync(preTrainedData.ConfigFileUrl, destinationPath).ConfigureAwait(false))
            {
                return false;
            }

            if (!await DownloadAsync(preTrainedData.NamesFileUrl, destinationPath).ConfigureAwait(false))
            {
                return false;
            }

            if (!await DownloadAsync(preTrainedData.WeightsFileUrl, destinationPath).ConfigureAwait(false))
            {
                return false;
            }

            if (preTrainedData.OptionalFileUrls != null)
            {
                foreach (var optionalFile in preTrainedData.OptionalFileUrls)
                {
                    if (!await DownloadAsync(optionalFile, destinationPath).ConfigureAwait(false))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private async Task<bool> DownloadAsync(string url, string destinationPath)
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);
            var filePath = Path.Combine(destinationPath, fileName);

            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromMinutes(30);

                using (var httpResponseMessage = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    if (!httpResponseMessage.IsSuccessStatusCode)
                    {
                        return false;
                    }

                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Exists)
                    {
                        if (fileInfo.Length == httpResponseMessage.Content.Headers.ContentLength)
                        {
                            return true;
                        }
                        else
                        {
                            File.Delete(filePath);
                        }
                    }

                    var fileContentStream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    using (var sourceStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                    {
                        await fileContentStream.CopyToAsync(sourceStream).ConfigureAwait(false);
                    }

                    return true;
                }
            }
        }
    }
}
