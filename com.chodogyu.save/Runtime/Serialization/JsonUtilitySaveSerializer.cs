using System;
using System.Text;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.Save.Serialization
{
    /// <summary>
    /// Unity의 JsonUtility를 사용하여 저장 데이터를 JSON 기반 UTF-8 바이트 배열로 변환합니다.
    /// JsonUtility가 지원하는 직렬화 규칙을 따르며 파일 저장이나 경로 관리는 담당하지 않습니다.
    /// </summary>
    public sealed class JsonUtilitySaveSerializer : ISaveSerializer
    {
        private static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding(false, true);

        /// <summary>
        /// 지정한 저장 데이터를 JSON으로 변환한 뒤 UTF-8 바이트 배열로 직렬화합니다.
        /// </summary>
        public Result<byte[]> Serialize<T>(T data)
        {
            if (data is null)
            {
                return Result<byte[]>.Failure(new ResultError(
                    SaveErrorCodes.InvalidData,
                    "직렬화할 저장 데이터는 null일 수 없습니다."));
            }

            try
            {
                string json = JsonUtility.ToJson(data);
                byte[] bytes = Utf8Encoding.GetBytes(json);

                return Result<byte[]>.Success(bytes);
            }
            catch (Exception exception)
            {
                return Result<byte[]>.Failure(new ResultError(
                    SaveErrorCodes.SerializationFailed,
                    $"저장 데이터를 JSON으로 직렬화하지 못했습니다. {exception.Message}"));
            }
        }

        /// <summary>
        /// UTF-8 바이트 배열을 JSON 문자열로 변환한 뒤 지정한 저장 데이터 타입으로 역직렬화합니다.
        /// </summary>
        public Result<T> Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return Result<T>.Failure(new ResultError(
                    SaveErrorCodes.InvalidData,
                    "역직렬화할 저장 데이터는 null이거나 비어 있을 수 없습니다."));
            }

            try
            {
                string json = Utf8Encoding.GetString(data);
                T value = JsonUtility.FromJson<T>(json);

                if (value is null)
                {
                    return Result<T>.Failure(new ResultError(
                        SaveErrorCodes.DeserializationFailed,
                        "JSON 데이터에서 저장 데이터를 복원하지 못했습니다."));
                }

                return Result<T>.Success(value);
            }
            catch (Exception exception)
            {
                return Result<T>.Failure(new ResultError(
                    SaveErrorCodes.DeserializationFailed,
                    $"저장 데이터를 JSON에서 역직렬화하지 못했습니다. {exception.Message}"));
            }
        }
    }
}