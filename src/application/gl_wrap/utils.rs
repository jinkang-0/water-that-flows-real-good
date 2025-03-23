
/// Tie the calling of a closure to a variable
/// This is intended for partial initialization in unsafe contexts
pub struct ScopedClosure<F: FnOnce() -> ()> {
    f: Option<F>
}
impl<F: FnOnce() -> ()> ScopedClosure<F> {
    pub fn new(f: F) -> Self {
        Self { f: Some(f) }
    }
    pub fn forget(mut self) {
        let _ = self.f.take();
    }
}
impl<F: FnOnce() -> ()> Drop for ScopedClosure<F> {
    fn drop(&mut self) {
        if let Some(f) = self.f.take() {
            f();
        }
    }
}

