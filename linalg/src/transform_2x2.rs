
use super::Matrix;

pub fn rotate<T: num_traits::real::Real + Copy + std::ops::Neg<Output=T>>(t: T) -> Matrix<T, 2, 2> {
    Matrix::from([
        [t.cos(), -t.sin() ],
        [t.sin()  ,  t.cos()],
    ]).as_transpose()
}

