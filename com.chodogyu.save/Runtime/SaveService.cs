using System;
using CDG.Core.Results;
using CDG.Save.Serialization;
using CDG.Save.Storage;

namespace CDG.Save
{
    /// <summary>
    /// 저장 데이터의 직렬화와 Storage 접근을 조정하는 Save / Load 서비스입니다.
    /// 실제 파일 형식과 저장 매체 구현은 각각 Serializer와 Storage에 위임합니다.
    /// </summary>
    public sealed class SaveService
    {
        private readonly ISaveStorage storage;
        private readonly ISaveSerializer serializer;

        /// <summary>
        /// 지정한 Storage와 Serializer를 사용하여 SaveService를 생성합니다.
        /// </summary>
        /// <param name="storage">직렬화된 데이터를 저장하고 읽을 Storage입니다.</param>
        /// <param name="serializer">저장 데이터를 직렬화하고 역직렬화할 Serializer입니다.</param>
        /// <exception cref="ArgumentNullException">
        /// Storage 또는 Serializer가 null인 경우 발생합니다.
        /// </exception>
        public SaveService(ISaveStorage storage, ISaveSerializer serializer)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        internal Result WriteCopies(SaveSlot slot, byte[] data)
        {
            Result primaryResult = storage.Write(slot, SaveStorageCopy.Primary, data);

            if (primaryResult.IsFailure)
            {
                return primaryResult;
            }

            Result backupResult = storage.Write(slot, SaveStorageCopy.Backup, data);

            if (backupResult.IsFailure)
            {
                return Result.Failure(new ResultError(
                    SaveErrorCodes.BackupWriteFailed,
                    $"기본 저장 파일은 기록되었지만 백업 저장 파일 기록에 실패했습니다. {backupResult.Error.Message}"));
            }

            return Result.Success();
        }
    }
}