
use crate::matrix::Matrix;

/// Column Vector
/// "N tall vector"
/// `┬ 🮽 `
/// `N 🮽 `
/// `│  ⋮`
/// `┴ 🮽 `
///
pub type Vector<T, const N: usize> = Matrix<T, 1, N>;

impl<T:Copy + std::ops::Mul<Output=T> + std::ops::Add<Output=T> + num_traits::real::Real, const N: usize> Vector<T, N> {
    pub fn norm(&self) -> T {
        self.data[0].iter().map(|a| (*a) * (*a)).reduce(|a, b| a + b).expect("Vector norm failed to reduce").sqrt()
    }
}

impl<T: Copy, const N: usize> Vector<T, N> {
    pub fn x(&self) -> T { self.data[0][0] }
    pub fn y(&self) -> T { self.data[0][1] }
    pub fn z(&self) -> T { self.data[0][2] }
    pub fn w(&self) -> T { self.data[0][3] }

    pub fn r(&self) -> T { self.data[0][0] }
    pub fn g(&self) -> T { self.data[0][1] }
    pub fn b(&self) -> T { self.data[0][2] }
    pub fn a(&self) -> T { self.data[0][3] }
}

impl<T, const N: usize> Vector<T, N> {
    pub fn x_ref(&self) -> &T { &self.data[0][0] }
    pub fn y_ref(&self) -> &T { &self.data[0][1] }
    pub fn z_ref(&self) -> &T { &self.data[0][2] }
    pub fn w_ref(&self) -> &T { &self.data[0][3] }

    pub fn r_ref(&self) -> &T { &self.data[0][0] }
    pub fn g_ref(&self) -> &T { &self.data[0][1] }
    pub fn b_ref(&self) -> &T { &self.data[0][2] }
    pub fn a_ref(&self) -> &T { &self.data[0][3] }


    pub fn x_mut(&mut self) -> &mut T { &mut self.data[0][0] }
    pub fn y_mut(&mut self) -> &mut T { &mut self.data[0][1] }
    pub fn z_mut(&mut self) -> &mut T { &mut self.data[0][2] }
    pub fn w_mut(&mut self) -> &mut T { &mut self.data[0][3] }

    pub fn r_mut(&mut self) -> &mut T { &mut self.data[0][0] }
    pub fn g_mut(&mut self) -> &mut T { &mut self.data[0][1] }
    pub fn b_mut(&mut self) -> &mut T { &mut self.data[0][2] }
    pub fn a_mut(&mut self) -> &mut T { &mut self.data[0][3] }
}

pub fn dot<T: Copy + std::ops::Mul<Output = T> + std::ops::Add<Output = T>, const N: usize>(v1: &Vector<T, N>, v2: &Vector<T, N>) -> T {
    v1.data[0].iter().zip(v2.data[0].iter()).map(|(a, b)| (*a) * (*b)).reduce(|a, b| a + b).unwrap()
}

