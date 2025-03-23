
use super::Matrix;

pub fn rotate_x<T: num_traits::Zero + num_traits::real::Real + Copy + std::ops::Neg<Output=T>>(t: T) -> Matrix<T, 3, 3> {
    Matrix::from([
        [T::one() , T::zero() ,  T::zero() ],
        [T::zero(), t.cos(),  -t.sin() ],
        [T::zero(), t.sin()  ,  t.cos()]
    ]).as_transpose()
}
pub fn rotate_y<T: num_traits::Zero + num_traits::real::Real + Copy + std::ops::Neg<Output=T>>(t: T) -> Matrix<T, 3, 3> {
    Matrix::from([
        [ t.cos(), T::zero(), t.sin()  ],
        [ T::zero() , T::one() , T::zero() ],
        [-t.sin()  , T::zero(), t.cos()]
    ]).as_transpose()
}
pub fn rotate_z<T: num_traits::Zero + num_traits::real::Real + Copy + std::ops::Neg<Output=T>>(t: T) -> Matrix<T, 3, 3> {
    Matrix::from([
        [t.cos(), -t.sin()  , T::zero()],
        [t.sin()  ,  t.cos(), T::zero()],
        [T::zero() ,  T::zero() , T::one() ]
    ]).as_transpose()
}

