
use super::Matrix;

pub fn translate<T: num_traits::Zero + num_traits::One + Copy>(x: T, y: T, z: T) -> Matrix<T, 4, 4> {
    Matrix::from([
        [T::one() , T::zero(), T::zero(), x       ],
        [T::zero(), T::one() , T::zero(), y       ],
        [T::zero(), T::zero(), T::one() , z       ],
        [T::zero(), T::zero(), T::zero(), T::one()]
    ]).as_transpose()
}

pub fn rotate_x<T: num_traits::real::Real + Copy + std::ops::Neg<Output=T>>(t: T) -> Matrix<T, 4, 4> {
    Matrix::from([
        [T::one() , T::zero() ,   T::zero() , T::zero()],
        [T::zero(), t.cos(),  -t.sin()  , T::zero()],
        [T::zero(), t.sin()  ,   t.cos(), T::zero()],
        [T::zero(), T::zero() ,   T::zero() , T::one() ]
    ]).as_transpose()
}
pub fn rotate_y<T: num_traits::real::Real + Copy + std::ops::Neg<Output=T>>(t: T) -> Matrix<T, 4, 4> {
    Matrix::from([
        [ t.cos(), T::zero(),  t.sin()  , T::zero()],
        [ T::zero() , T::one() ,  T::zero() , T::zero()],
        [-t.sin()  , T::zero(),  t.cos(), T::zero()],
        [ T::zero() , T::zero(),  T::zero() , T::one() ]
    ]).as_transpose()
}
pub fn rotate_z<T: num_traits::real::Real + Copy + std::ops::Neg<Output=T>>(t: T) -> Matrix<T, 4, 4> {
    Matrix::from([
        [t.cos(), -t.sin() ,  T::zero(), T::zero()],
        [t.sin()  ,  t.cos(), T::zero(), T::zero()],
        [T::zero() ,  T::zero() , T::one() , T::zero()],
        [T::zero() ,  T::zero() , T::zero(), T::one() ]
    ]).as_transpose()
}

/// Transformation matrix from world coordinates to clip space coordinates
/// perspective divide visualization: [https://www.desmos.com/calculator/nrv1bs0lih](https://www.desmos.com/calculator/nrv1bs0lih)
/// Swaps Y and Z such that Z represents the vertical world axis
pub fn perspective<T: num_traits::real::Real + Copy>(vertical_fov: T, aspect: T, near: T, far: T) -> Matrix<T, 4, 4>
    where
        T: std::ops::Add<T, Output=T>,
        T: std::ops::Sub<T, Output=T>,
        T: std::ops::Mul<T, Output=T>,
        T: std::ops::Div<T, Output=T>,
        T: std::ops::Neg<Output=T>,
{
    let tan_fov_inv = T::one() / (vertical_fov / (T::one() + T::one())).tan();
    Matrix::from([
        [tan_fov_inv / aspect, T::zero()     ,  T::zero()   , T::zero()             ],
        [T::zero()           , T::zero()     , -tan_fov_inv , T::zero()             ],
        [T::zero()           , far/(far-near),  T::zero()   , -(far*near)/(far-near)],
        [T::zero()           , T::one()      ,  T::zero()   , T::zero()             ]
    ]).as_transpose()
}

/// Orthographic matrix from world coordinates to clip space coordinates
/// Swaps Y and Z such that Z represents the vertical world axis
pub fn orthographic<T: num_traits::real::Real + Copy>(width: T, height: T, depth: T) -> Matrix<T, 4, 4>
    where
        T: std::ops::Add<T, Output=T>,
        T: std::ops::Sub<T, Output=T>,
        T: std::ops::Mul<T, Output=T>,
        T: std::ops::Div<T, Output=T>,
        T: std::ops::Neg<Output=T>,
{
    Matrix::from([
        [(T::one() + T::one()) / width, T::zero()                    ,  T::zero()                     , T::zero()],
        [T::zero()                    , T::zero()                    , -(T::one() + T::one()) / height, T::zero()],
        [T::zero()                    , (T::one() + T::one()) / depth,  T::zero()                     , T::zero()],
        [T::zero()                    , T::zero()                    ,  T::zero()                     , T::one() ]
    ]).as_transpose()
}

/// Transformation matrix from pixel screen space to world space
/// Swaps Y and Z such that Z represents the vertical world axis in world space
/// Z represents depth in screen space
/// intended usage:
/// ```glsl
/// mat4 T = perspective_ray_gen(f, w, h, n, f);
/// vec4 ray_a = T * vec4(pixel_x, pixel_y, 0.0, 1.0);
/// vec4 ray_b = T * vec4(pixel_x, pixel_y, 1.0, 1.0);
/// ray_a.xyz /= ray_a.w; // to normalized coordinates
/// ray_b.xyz /= ray_b.w;
/// vec3 ray_origin = ray_a.xyz;
/// vec3 ray_direction = (ray_b - ray_a).xyz;
/// ```
pub fn perspective_ray_gen<T: num_traits::real::Real + Copy>(vertical_fov: T, width_px: T, height_px: T, near: T, far: T) -> Matrix<T, 4, 4>
    where
        T: std::ops::Add<T, Output=T>,
        T: std::ops::Sub<T, Output=T>,
        T: std::ops::Mul<T, Output=T>,
        T: std::ops::Div<T, Output=T>,
        T: std::ops::Neg<Output=T>,
{
    let aspect = width_px / height_px;
    let tan_fov = (vertical_fov / (T::one() + T::one())).tan();
    Matrix::from([
        [tan_fov * (T::one() + T::one()) / height_px, T::zero()                                  ,  T::zero()            , -tan_fov * aspect],
        [T::zero()                                  , T::zero()                                  ,  T::zero()            , T::one()         ],
        [T::zero()                                  ,-(T::one() + T::one()) * tan_fov / height_px,  T::zero()            ,  tan_fov         ],
        [T::zero()                                  , T::zero()                                  , -(far-near)/(far*near), far/(far*near)   ]
    ]).as_transpose()
}

