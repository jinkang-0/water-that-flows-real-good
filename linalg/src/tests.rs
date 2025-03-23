
#[cfg(test)]
mod matrix_tests {
    use crate::*;

    #[test]
    fn test_eq() {
        let result1 = Matrix::<i32, 1, 8>::from([[0,  1, 2, 3, 4, 5, 6, 7]]);
        let result2 = Matrix::<i32, 1, 8>::from([[0,  1, 2, 3, 4, 5, 6, 7]]);
        let result3 = Matrix::<i32, 1, 8>::from([[0, -1, 2, 3, 4, 5, 6, 7]]);
        assert_eq!(result1, result2);
        assert_ne!(result2, result3);
        assert_ne!(result1, result3);
        let result1 = Matrix::<i32, 6, 3>::from([[-9, -8, -7], [-6, -5, -4], [-3, -2, -1], [ 0, 1, 2], [3, 4, 5], [6, 7, 8]]);
        let result2 = Matrix::<i32, 6, 3>::from([[-9, -8, -7], [-6, -5, -4], [-3, -2, -1], [-0, 1, 2], [3, 4, 5], [6, 7, 8]]);
        let result3 = Matrix::<i32, 6, 3>::from([[-9, -8, -7], [-6, -5, -4], [-3, -2, -1], [ 0, 1, 2], [3, 4, 4], [6, 7, 8]]);
        let result4 = Matrix::<i32, 6, 3>::from([[-9, -8, -7], [-6, -5, -4], [-3, -2, -1], [ 0, 1, 2], [4, 4, 5], [6, 7, 8]]);
        assert_eq!(result1, result2);
        assert_ne!(result2, result3);
        assert_ne!(result1, result3);
        assert_ne!(result2, result4);
        assert_ne!(result1, result4);
        assert_ne!(result3, result4);
        let result1 = Matrix::<f32, 6, 3>::from([[-9.0, -8.0, -7.0], [-6.0, -5.0, -4.0], [-3.0, -2.0, -1.0], [ 0.0, 1.0, 2.0], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]]);
        let result2 = Matrix::<f32, 6, 3>::from([[-9.0, -8.0, -7.0], [-6.0, -5.0, -4.0], [-3.0, -2.0, -1.0], [-0.0, 1.0, 2.0], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]]);
        let result3 = Matrix::<f32, 6, 3>::from([[-9.0, -8.0, -7.0], [-6.0, -5.0, -4.0], [-3.0, -2.0, -1.0], [ 0.0, 1.0, 2.0], [3.0, 4.0, 4.0], [6.0, 7.0, 8.0]]);
        let result4 = Matrix::<f32, 6, 3>::from([[-9.0, -8.0, -7.0], [-6.0, -5.0, -4.0], [-3.0, -2.0, -1.0], [ 0.0, 1.0, 2.0], [4.0, 4.0, 5.0], [6.0, 7.0, 8.0]]);
        assert_eq!(result1, result2);
        assert_ne!(result2, result3);
        assert_ne!(result1, result3);
        assert_ne!(result2, result4);
        assert_ne!(result1, result4);
        assert_ne!(result3, result4);
    }

    #[test]
    fn test_zeros() {
        let i1 = Matrix::<f32, 5, 5>::zeros();
        let i2 = Matrix::<f32, 5, 5>::from([[-0.0,0.0,0.0,0.0,0.0], [0.0,0.0,-0.0,0.0,0.0], [0.0,0.0,0.0,0.0,-0.0], [0.0,0.0,0.0,-0.0,0.0], [-0.0,0.0,0.0,0.0,0.0]]);
        assert_eq!(i1, i2);
    }
    #[test]
    fn test_identity() {
        let i1 = Matrix::<f32, 5, 5>::identity();
        let i2 = Matrix::<f32, 5, 5>::from([[1.0,0.0,0.0,0.0,0.0], [0.0,1.0,0.0,0.0,0.0], [0.0,0.0,1.0,0.0,0.0], [0.0,0.0,0.0,1.0,0.0], [0.0,0.0,0.0,0.0,1.0]]);
        assert_eq!(i1, i2);
    }

    #[test]
    fn test_matmul() {
        let a = Matrix::<i32, 3, 5>::from([[1, 4, 7, 10, 13], [2, 5, 8, 11, 14], [3, 6, 9, 12, 15]]); // 5x3
        let b = Matrix::<i32, 4, 3>::from([[16, 20, 24], [17, 21, 25], [18, 22, 26], [19, 23, 27]]); // 3x4
        let c_expected = Matrix::<i32, 4, 5>::from([[128, 308, 488, 668, 848], [134, 323, 512, 701, 890], [140, 338, 536, 734, 932], [146, 353, 560, 767, 974]]); // 5x4
        let c_actual = matmul(&a, &b);
        assert_eq!(c_expected, c_actual);
    }

    #[test]
    fn test_transpose() {
        let a = Matrix::<i32, 4, 2>::from([[1, 2], [3, 4], [5, 6], [7, 8]]);
        let a_t = Matrix::<i32, 2, 4>::from([[1, 3, 5, 7], [2, 4, 6, 8]]);
        assert_eq!(a.as_transpose(), a_t);
    }
}

