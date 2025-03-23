
/// Column major matrix
/// "N by M matrix"
/// `  ├───N───┤`
/// `┬ 🮽 🮽 🮽 …🮽 `
/// `M 🮽 🮽 🮽 …🮽 `
/// `│  ⋮ ⋮ ⋮⋱ ⋮`
/// `┴ 🮽 🮽 🮽 …🮽 `
///
#[derive(Copy, Clone, Hash, Debug)]
//#[repr(C)]
//#[repr(align(16))]
pub struct Matrix<T, const N: usize, const M: usize> {
    pub data: [[T; M]; N]
}

impl<T: PartialEq, const N: usize, const M: usize> PartialEq for Matrix<T, N, M> {
    fn eq(&self, other: &Self) -> bool {
        self.data.iter().flatten().zip(other.data.iter().flatten()).all(|(a, b)| a == b)
    }
}
impl<T: Eq, const N: usize, const M: usize> Eq for Matrix<T, N, M> {}
impl<T, const N: usize, const M: usize> From<[[T; M]; N]> for Matrix<T, N, M> {
    fn from(data: [[T; M]; N]) -> Self {
        Self { data }
    }
}
impl<T, const N: usize> From<[T; N]> for Matrix<T, 1, N> {
    fn from(data: [T; N]) -> Self {
        Self { data: [data] }
    }
}
impl<T2, const N: usize, const M: usize> Matrix<T2, N, M> {
    pub fn cast_from<T1: Copy + Into<T2>>(src: &Matrix<T1, N, M>) -> Self {
        std::array::from_fn::<[T2; M], N, _>(|j| { // `j`th column of source
            std::array::from_fn::<T2, M, _>(|i| { // `i`th row of source
                src.data[j][i].into()
            })
        }).into()
    }
    pub fn try_cast_from_or_unwrap<T1: Copy + TryInto<T2, Error: std::fmt::Debug>>(src: &Matrix<T1, N, M>) -> Self 
    {
        std::array::from_fn::<[T2; M], N, _>(|j| { // `j`th column of source
            std::array::from_fn::<T2, M, _>(|i| { // `i`th row of source
                src.data[j][i].try_into().unwrap()
            })
        }).into()
    }
}

impl<T, const N: usize, const M: usize> Matrix<T, N, M> {
    pub const fn const_from_array2(data: [[T; M]; N]) -> Self {
        Self { data }
    }
}
impl<T, const N: usize> Matrix<T, 1, N> {
    pub const fn const_from_array1(data: [T; N]) -> Self {
        Self { data: [data] }
    }
}

impl<T: num_traits::Zero + Copy, const N: usize, const M: usize> Matrix<T, N, M> {
    pub fn zeros() -> Self {
        Self::from([[T::zero(); M]; N])
    }
}
impl<T: num_traits::One + num_traits::Zero + Copy, const N: usize> Matrix<T, N, N> {
    pub fn identity() -> Self {
        let mut data = [[T::zero(); N]; N];
        for i in 0..N { data[i][i] = T::one() }
        Self::from(data)
    }
}

impl<T: Copy, const N: usize, const M: usize> Matrix<T, N, M> {
    pub fn as_transpose(&self) -> Matrix<T, M, N> {
        std::array::from_fn::<[T; N], M, _>(|j| { // `j`th column of source
            std::array::from_fn::<T, N, _>(|i| { // `i`th row of source
                self.data[i][j]
            })
        }).into()
    }
}
///////////////////
//// OPERATORS ////
///////////////////

// Add with scalar
impl<T: Copy + std::ops::AddAssign, const N: usize, const M: usize>
std::ops::AddAssign<T> for Matrix<T, N, M> {
    fn add_assign(&mut self, rhs: T) {
        for j in 0..N { // `j`th col
            for i in 0..M { // `i`th rowAddAssign
                self.data[j][i] += rhs;
            }
        }
    }
}
impl<T: Copy + std::ops::Add<Output = T>, const N: usize, const M: usize>
std::ops::Add<T> for Matrix<T, N, M> {
    type Output = Self;
    fn add(self, rhs: T) -> Self {
        std::array::from_fn::<[T; M], N, _>(|j| { // `j`th col
            std::array::from_fn::<T, M, _>(|i| { // `i`th row
                self.data[j][i] + rhs
            })
        }).into()
    }
}
// Mul with scalar
impl<T: Copy + std::ops::MulAssign, const N: usize, const M: usize>
std::ops::MulAssign<T> for Matrix<T, N, M> {
    fn mul_assign(&mut self, rhs: T) {
        for j in 0..N { // `j`th col
            for i in 0..M { // `i`th rowAddAssign
                self.data[j][i] *= rhs;
            }
        }
    }
}
impl<T: Copy + std::ops::Mul<Output = T>, const N: usize, const M: usize>
std::ops::Mul<T> for Matrix<T, N, M> {
    type Output = Self;
    fn mul(self, rhs: T) -> Self {
        std::array::from_fn::<[T; M], N, _>(|j| { // `j`th col
            std::array::from_fn::<T, M, _>(|i| { // `i`th row
                self.data[j][i] * rhs
            })
        }).into()
    }
}
// Div with scalar
impl<T: Copy + std::ops::DivAssign, const N: usize, const M: usize>
std::ops::DivAssign<T> for Matrix<T, N, M> {
    fn div_assign(&mut self, rhs: T) {
        for j in 0..N { // `j`th col
            for i in 0..M { // `i`th rowAddAssign
                self.data[j][i] /= rhs;
            }
        }
    }
}
impl<T: Copy + std::ops::Div<Output = T>, const N: usize, const M: usize>
std::ops::Div<T> for Matrix<T, N, M> {
    type Output = Self;
    fn div(self, rhs: T) -> Self {
        std::array::from_fn::<[T; M], N, _>(|j| { // `j`th col
            std::array::from_fn::<T, M, _>(|i| { // `i`th row
                self.data[j][i] / rhs
            })
        }).into()
    }
}

// Add with Matrix
impl<T: Copy + std::ops::AddAssign, const N: usize, const M: usize>
std::ops::AddAssign for Matrix<T, N, M> {
    fn add_assign(&mut self, rhs: Self) {
        for j in 0..N { // `j`th col
            for i in 0..M { // `i`th rowAddAssign
                self.data[j][i] += rhs.data[j][i];
            }
        }
    }
}
impl<T: Copy + std::ops::Add<Output = T>, const N: usize, const M: usize>
std::ops::Add for Matrix<T, N, M> {
    type Output = Self;
    fn add(self, rhs: Self) -> Self {
        std::array::from_fn::<[T; M], N, _>(|j| { // `j`th col
            std::array::from_fn::<T, M, _>(|i| { // `i`th row
                self.data[j][i] + rhs.data[j][i]
            })
        }).into()
    }
}

// Mul with Matrix
impl<T: Copy + std::ops::Mul<Output = T> + std::ops::Add<Output = T>, const M: usize, const N: usize, const O: usize>
std::ops::Mul<Matrix<T, O, N>> for Matrix<T, N, M> {
    type Output = Matrix<T, O, M>;
    fn mul(self, rhs: Matrix<T, O, N>) -> Matrix<T, O, M> {
        matmul(&self, &rhs)
    }
}
// MulAssign with Matrix
// TODO //

/// `  ├───N───┤      ├───O───┤     ├───O───┤`
/// `┬ 🮽 🮽 🮽 …🮽 ┬   ┬ 🮽 🮽 🮽 …🮽    ┬ 🮽 🮽 🮽 …🮽 `
/// `M 🮽 🮽 🮽 …🮽 M * N 🮽 🮽 🮽 …🮽  = M 🮽 🮽 🮽 …🮽 `
/// `│  ⋮ ⋮ ⋮⋱ ⋮│   │  ⋮ ⋮ ⋮⋱ ⋮   │  ⋮ ⋮ ⋮⋱ ⋮`
/// `┴ 🮽 🮽 🮽 …🮽 ┴   ┴ 🮽 🮽 🮽 …🮽    ┴ 🮽 🮽 🮽 …🮽 `
pub fn matmul<T: Copy + std::ops::Mul<Output = T> + std::ops::Add<Output = T>, const M: usize, const N: usize, const O: usize>(lhs: &Matrix<T, N, M>, rhs: &Matrix<T, O, N>) -> Matrix<T, O, M> {
    let data: [[T; M]; O] = std::array::from_fn::<[T; M], O, _>(|j| { // `j in 0..O` (`j`th column)
        std::array::from_fn::<T, M, _>(|i| { // `i in 0..M` (`i`th row)
            // compute output value at `i,j`
            let lhs_row = lhs.data.iter().map(|row| row[i]); // `i`th row of `lhs`
            let rhs_col = rhs.data[j].iter(); // `j`th col of `rhs`
            lhs_row.zip(rhs_col).map(|(a, b)| a * (*b)).reduce(|a, b| a + b).unwrap() // compute dot product
        })
    });
    Matrix::<T, O, M>::from(data)
}

