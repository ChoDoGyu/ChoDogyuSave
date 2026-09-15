namespace CDG.Save
{
    /// <summary>
    /// 저장 데이터 불러오기 결과의 상태를 나타냅니다.
    /// </summary>
    public enum SaveLoadStatus
    {
        /// <summary>
        /// 기본 저장 파일과 백업 파일 모두 존재하지 않아 불러올 데이터가 없는 상태입니다.
        /// </summary>
        NotFound = 0,

        /// <summary>
        /// 기본 저장 파일에서 데이터를 정상적으로 불러온 상태입니다.
        /// </summary>
        LoadedFromPrimary = 1,

        /// <summary>
        /// 기본 저장 파일을 사용할 수 없어 백업 파일에서 데이터를 불러온 상태입니다.
        /// </summary>
        LoadedFromBackup = 2
    }
}