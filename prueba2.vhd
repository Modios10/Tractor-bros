library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity prueba2 is
    Port (  
        boton       : in  std_logic; 
        boton2      : in  std_logic; 
        led_azul    : out STD_LOGIC; 
        led_rojo    : out std_logic;
        clk         : in  std_logic;
        uart_tx     : out std_logic;
        uart_rx_pin : in  std_logic;
        -- Tus nombres específicos para el display externo
        dB, dF, dA, dG, dC, dDP, dD, dE : OUT std_logic
    );
end prueba2;

architecture rtl of prueba2 is
    signal btn1_clean, btn2_clean : std_logic;
    signal previous1, previous2   : std_logic := '1';
    signal send_flag, busy        : std_logic;
    signal data_to_send           : std_logic_vector(7 downto 0);

    signal rx_data      : std_logic_vector(7 downto 0);
    signal rx_ready     : std_logic;
    signal counter      : integer range 0 to 9 := 0;
    signal segments     : std_logic_vector(6 downto 0); -- abcdefg

begin

    -- Instancias de tus archivos externos
    DB1: entity work.debounce port map(clk => clk, sig_in => boton, sig_stable => btn1_clean);
    DB2: entity work.debounce port map(clk => clk, sig_in => boton2, sig_stable => btn2_clean);
    TX_UNIT: entity work.uart_tx port map(clk => clk, start => send_flag, data_in => data_to_send, tx_line => uart_tx, busy => busy);
    RX_UNIT: entity work.uart_rx port map(clk => clk, rx_line => uart_rx_pin, data_out => rx_data, new_data_tick => rx_ready);

    process(clk)
    begin
        if rising_edge(clk) then
            -- Lógica de LEDs intacta
            led_azul <= not btn1_clean;
            led_rojo <= not btn2_clean;

            -- Transmisión UART
            if (previous1 = '1' and btn1_clean = '0' and busy = '0') then
                data_to_send <= x"41"; -- Envía 'A'
                send_flag    <= '1';
            elsif (previous2 = '1' and btn2_clean = '0' and busy = '0') then
                data_to_send <= x"50"; -- Envía 'P'
                send_flag    <= '1';
            else
                send_flag <= '0';
            end if;
            previous1 <= btn1_clean;
            previous2 <= btn2_clean;

            -- Lógica del contador por Recepción UART
            if rx_ready = '1' then
                if rx_data = x"50" then -- Si llega 'P'
                    if counter < 9 then counter <= counter + 1; end if;
                elsif rx_data = x"41" then -- Si llega 'A'
                    if counter > 0 then counter <= counter - 1; end if;
                end if;
            end if;
        end if;
    end process;

    -- Decodificador de 7 segmentos 
    process(counter)
    begin
        case counter is
            when 0 => segments <= "1111110"; -- abcdefg
            when 1 => segments <= "0110000";
            when 2 => segments <= "1101101";
            when 3 => segments <= "1111001";
            when 4 => segments <= "0110011";
            when 5 => segments <= "1011011";
            when 6 => segments <= "1011111";
            when 7 => segments <= "1110000";
            when 8 => segments <= "1111111";
            when 9 => segments <= "1111011";
            when others => segments <= "0000000";
        end case;
    end process;

    -- Mapeo a tus pines de salida
    dA <= segments(6); dB <= segments(5); dC <= segments(4); dD <= segments(3);
    dE <= segments(2); dF <= segments(1); dG <= segments(0); dDP <= '0';

end rtl;