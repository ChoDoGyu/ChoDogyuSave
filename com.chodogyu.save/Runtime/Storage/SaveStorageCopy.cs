namespace CDG.Save.Storage
{
    /// <summary>
    /// 하나의 저장 슬롯에서 접근할 저장 파일의 종류를 나타냅니다.
    /// 임시 파일은 Storage 구현 내부 세부사항이므로 포함하지 않습니다.
    /// </summary>
    public enum SaveStorageCopy
    {
        /// <summary>
        /// 정상적인 저장 및 불러오기에서 우선 사용하는 기본 저장 파일입니다.
        /// </summary>
        Primary = 0,

        /// <summary>
        /// 기본 저장 파일을 사용할 수 없을 때 복구 용도로 사용하는 백업 저장 파일입니다.
        /// </summary>
        Backup = 1
    }
}