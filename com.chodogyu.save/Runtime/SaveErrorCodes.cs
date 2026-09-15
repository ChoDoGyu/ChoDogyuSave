namespace CDG.Save
{
    /// <summary>
    /// Save Framework에서 발생할 수 있는 오류를 식별하기 위한 오류 코드입니다.
    /// 실제 오류 설명은 <c>ResultError.Message</c>를 통해 함께 제공합니다.
    /// </summary>
    public static class SaveErrorCodes
    {
        /// <summary>
        /// 저장 또는 직렬화할 데이터가 유효하지 않은 경우 사용합니다.
        /// </summary>
        public const string InvalidData = "save.invalid_data";

        /// <summary>
        /// 저장 데이터를 직렬화하지 못한 경우 사용합니다.
        /// </summary>
        public const string SerializationFailed = "save.serialization_failed";

        /// <summary>
        /// 저장 데이터를 역직렬화하지 못한 경우 사용합니다.
        /// </summary>
        public const string DeserializationFailed = "save.deserialization_failed";

        /// <summary>
        /// 요청한 저장 파일이 존재하지 않는 경우 Storage 계층에서 사용합니다.
        /// </summary>
        public const string StorageNotFound = "save.storage_not_found";

        /// <summary>
        /// 저장 파일을 읽는 과정에서 오류가 발생한 경우 사용합니다.
        /// </summary>
        public const string StorageReadFailed = "save.storage_read_failed";

        /// <summary>
        /// 기본 저장 파일을 기록하는 과정에서 오류가 발생한 경우 사용합니다.
        /// </summary>
        public const string StorageWriteFailed = "save.storage_write_failed";

        /// <summary>
        /// 기본 저장 파일 기록 후 백업 파일 기록에 실패한 경우 사용합니다.
        /// </summary>
        public const string BackupWriteFailed = "save.backup_write_failed";

        /// <summary>
        /// 저장 파일을 삭제하는 과정에서 오류가 발생한 경우 사용합니다.
        /// </summary>
        public const string StorageDeleteFailed = "save.storage_delete_failed";

        /// <summary>
        /// 저장 데이터가 손상되어 정상적인 복원이 불가능한 경우 사용합니다.
        /// </summary>
        public const string CorruptedData = "save.corrupted_data";
    }
}