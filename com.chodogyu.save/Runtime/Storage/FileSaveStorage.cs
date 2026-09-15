using System;
using System.IO;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.Save.Storage
{
    /// <summary>
    /// 로컬 파일 시스템을 사용하여 직렬화된 저장 데이터를 관리하는 Storage 구현입니다.
    /// 기본 저장 위치는 Application.persistentDataPath 아래의 Saves 폴더이며 생성자에서 다른 루트 경로를 지정할 수 있습니다.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private const string PrimaryExtension = ".save";
        private const string BackupExtension = ".save.bak";
        private const string TempExtension = ".save.tmp";

        private readonly string rootPath;

        /// <summary>
        /// 기본 저장 루트 경로를 사용하여 FileSaveStorage를 생성합니다.
        /// </summary>
        public FileSaveStorage() : this(Path.Combine(Application.persistentDataPath, "Saves"))
        {
        }

        /// <summary>
        /// 지정한 루트 경로를 사용하여 FileSaveStorage를 생성합니다.
        /// </summary>
        /// <param name="rootPath">저장 파일을 관리할 루트 디렉터리 경로입니다.</param>
        /// <exception cref="ArgumentException">
        /// 루트 경로가 null, 빈 문자열 또는 공백으로만 구성된 경우 발생합니다.
        /// </exception>
        public FileSaveStorage(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("저장 루트 경로는 비어 있을 수 없습니다.", nameof(rootPath));
            }

            this.rootPath = Path.GetFullPath(rootPath);
        }

        /// <inheritdoc />
        public Result Write(SaveSlot slot, SaveStorageCopy copy, byte[] data)
        {
            string path = GetPath(slot, copy);
            string tempPath = GetTempPath(slot);

            if (data == null)
            {
                return Result.Failure(new ResultError(
                    SaveErrorCodes.InvalidData,
                    "저장할 바이트 데이터는 null일 수 없습니다."));
            }

            try
            {
                Directory.CreateDirectory(rootPath);

                File.WriteAllBytes(tempPath, data);
                ReplaceFile(tempPath, path);

                return Result.Success();
            }
            catch (Exception exception)
            {
                TryDeleteTempFile(tempPath);

                return Result.Failure(new ResultError(
                    SaveErrorCodes.StorageWriteFailed,
                    $"저장 파일을 안전하게 기록하지 못했습니다. {exception.Message}"));
            }
        }

        /// <inheritdoc />
        public Result<byte[]> Read(SaveSlot slot, SaveStorageCopy copy)
        {
            string path = GetPath(slot, copy);

            try
            {
                byte[] data = File.ReadAllBytes(path);
                return Result<byte[]>.Success(data);
            }
            catch (FileNotFoundException)
            {
                return Result<byte[]>.Failure(new ResultError(
                    SaveErrorCodes.StorageNotFound,
                    "요청한 저장 파일이 존재하지 않습니다."));
            }
            catch (DirectoryNotFoundException)
            {
                return Result<byte[]>.Failure(new ResultError(
                    SaveErrorCodes.StorageNotFound,
                    "요청한 저장 파일이 존재하지 않습니다."));
            }
            catch (Exception exception)
            {
                return Result<byte[]>.Failure(new ResultError(
                    SaveErrorCodes.StorageReadFailed,
                    $"저장 파일을 읽지 못했습니다. {exception.Message}"));
            }
        }

        /// <inheritdoc />
        public Result<bool> Exists(SaveSlot slot, SaveStorageCopy copy)
        {
            string path = GetPath(slot, copy);

            try
            {
                FileAttributes attributes = File.GetAttributes(path);

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    return Result<bool>.Failure(new ResultError(
                        SaveErrorCodes.StorageReadFailed,
                        "저장 파일 경로에 파일이 아닌 디렉터리가 존재합니다."));
                }

                return Result<bool>.Success(true);
            }
            catch (FileNotFoundException)
            {
                return Result<bool>.Success(false);
            }
            catch (DirectoryNotFoundException)
            {
                return Result<bool>.Success(false);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(new ResultError(
                    SaveErrorCodes.StorageReadFailed,
                    $"저장 파일 존재 여부를 확인하지 못했습니다. {exception.Message}"));
            }
        }

        /// <inheritdoc />
        public Result Delete(SaveSlot slot, SaveStorageCopy copy)
        {
            string path = GetPath(slot, copy);

            try
            {
                File.Delete(path);
                return Result.Success();
            }
            catch (FileNotFoundException)
            {
                return Result.Success();
            }
            catch (DirectoryNotFoundException)
            {
                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(new ResultError(
                    SaveErrorCodes.StorageDeleteFailed,
                    $"저장 파일을 삭제하지 못했습니다. {exception.Message}"));
            }
        }

        internal string GetPath(SaveSlot slot, SaveStorageCopy copy)
        {
            ValidateSlot(slot);

            string extension = copy switch
            {
                SaveStorageCopy.Primary => PrimaryExtension,
                SaveStorageCopy.Backup => BackupExtension,
                _ => throw new ArgumentOutOfRangeException(nameof(copy), copy, "지원하지 않는 저장 파일 종류입니다.")
            };

            return Path.Combine(rootPath, slot.Name + extension);
        }

        internal string GetTempPath(SaveSlot slot)
        {
            ValidateSlot(slot);
            return Path.Combine(rootPath, slot.Name + TempExtension);
        }

        private static void ReplaceFile(string tempPath, string targetPath)
        {
            if (File.Exists(targetPath))
            {
                File.Replace(tempPath, targetPath, null);
                return;
            }

            File.Move(tempPath, targetPath);
        }

        private static void TryDeleteTempFile(string tempPath)
        {
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
            }
        }

        private static void ValidateSlot(SaveSlot slot)
        {
            if (slot == null)
            {
                throw new ArgumentNullException(nameof(slot));
            }
        }
    }
}