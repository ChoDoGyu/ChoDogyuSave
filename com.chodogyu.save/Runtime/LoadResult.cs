using System;

namespace CDG.Save
{
    /// <summary>
    /// 저장 데이터 불러오기 작업이 성공했을 때의 세부 결과를 나타냅니다.
    /// 데이터가 없는 정상 상태와 기본 또는 백업 파일에서 데이터를 불러온 상태를 구분합니다.
    /// </summary>
    /// <typeparam name="T">불러온 저장 데이터의 타입입니다.</typeparam>
    public sealed class LoadResult<T>
    {
        private readonly T _value;

        /// <summary>
        /// 저장 데이터 불러오기 상태입니다.
        /// </summary>
        public SaveLoadStatus Status { get; }

        /// <summary>
        /// 실제로 불러온 저장 데이터가 있는 상태인지를 나타냅니다.
        /// </summary>
        public bool IsFound => Status != SaveLoadStatus.NotFound;

        /// <summary>
        /// 불러온 저장 데이터를 반환합니다.
        /// 데이터가 없는 상태에서 접근하면 <see cref="InvalidOperationException"/>이 발생합니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="SaveLoadStatus.NotFound"/> 상태에서 Value에 접근한 경우 발생합니다.
        /// </exception>
        public T Value
        {
            get
            {
                if (!IsFound)
                {
                    throw new InvalidOperationException("불러온 저장 데이터가 없는 상태에서는 Value에 접근할 수 없습니다.");
                }

                return _value;
            }
        }

        private LoadResult(SaveLoadStatus status, T value)
        {
            Status = status;
            _value = value;
        }

        /// <summary>
        /// 저장 데이터가 존재하지 않는 정상 결과를 생성합니다.
        /// </summary>
        public static LoadResult<T> NotFound()
        {
            return new LoadResult<T>(SaveLoadStatus.NotFound, default);
        }

        /// <summary>
        /// 기본 저장 파일에서 데이터를 불러온 결과를 생성합니다.
        /// 참조 타입의 경우 null도 값으로 허용합니다.
        /// </summary>
        /// <param name="value">불러온 저장 데이터입니다.</param>
        public static LoadResult<T> FromPrimary(T value)
        {
            return new LoadResult<T>(SaveLoadStatus.LoadedFromPrimary, value);
        }

        /// <summary>
        /// 백업 저장 파일에서 데이터를 불러온 결과를 생성합니다.
        /// 참조 타입의 경우 null도 값으로 허용합니다.
        /// </summary>
        /// <param name="value">불러온 저장 데이터입니다.</param>
        public static LoadResult<T> FromBackup(T value)
        {
            return new LoadResult<T>(SaveLoadStatus.LoadedFromBackup, value);
        }
    }
}