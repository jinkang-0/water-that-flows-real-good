
mod matrix;
mod vector;

pub use matrix::Matrix;
pub use matrix::matmul;
pub use vector::Vector;
pub use vector::dot;

pub mod transform_4x4;
pub mod transform_3x3;
pub mod transform_2x2;

#[cfg(test)]
mod tests;

