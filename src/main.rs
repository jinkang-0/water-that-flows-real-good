
mod application;

fn main() {
    let mut app = application::Application::new(600, 400);
    println!("application initialized");
    app.run();
}

